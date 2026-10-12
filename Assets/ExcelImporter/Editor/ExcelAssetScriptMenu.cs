using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;
using System.Text.RegularExpressions;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

public class ExcelAssetScriptMenu
{
	const string ScriptTemplateName = "ExcelAssetScriptTemplete.cs.txt";

	[MenuItem("Assets/Create/ExcelAssetScript", false)]
	public static void CreateScript()
	{
		var selectedAssets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
		if (selectedAssets.Length == 0) return;

		string excelPath = AssetDatabase.GetAssetPath(selectedAssets[0]);
		string excelName = Path.GetFileNameWithoutExtension(excelPath);

		string defaultDirectory = Path.GetDirectoryName(excelPath);
		string savePath = EditorUtility.SaveFolderPanel("Save ExcelAssetScript", defaultDirectory, "");
		if (string.IsNullOrEmpty(savePath)) return;

		string path = Path.ChangeExtension(Path.Combine(savePath, excelName), "cs");
		CreateScriptFromExcel(excelPath, path);

		AssetDatabase.Refresh();
	}

	[MenuItem("Assets/Create/ExcelAssetScript", true)]
	static bool CreateScriptValidation()
	{
		var selectedAssets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
		if (selectedAssets.Length != 1) return false;
		var path = AssetDatabase.GetAssetPath(selectedAssets[0]);
		string ext = Path.GetExtension(path);
		return ext.Equals(".xls", StringComparison.OrdinalIgnoreCase) || ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
	}

	public static void CreateScriptFromExcel(string excelPath, string saveCsPath)
	{
		string excelName = Path.GetFileNameWithoutExtension(excelPath);
		string scriptString = BuildScriptString(excelName, excelPath);
		File.WriteAllText(saveCsPath, scriptString, Encoding.UTF8);
	}

	static string GetScriptTempleteString()
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		string[] filePath = Directory.GetFiles(currentDirectory, ScriptTemplateName, SearchOption.AllDirectories);
		if (filePath.Length == 0) throw new Exception("Script template not found.");

		string templateString = File.ReadAllText(filePath[0]);
		return templateString;
	}

	public static string BuildScriptString(string excelName, string excelPath)
	{
		string scriptString = GetScriptTempleteString();
		scriptString = scriptString.Replace("#ASSETSCRIPTNAME#", excelName);

		List<string> fieldLines = GetEntityFieldsFromExcel(excelPath);
		string fieldsString = string.Join("\n", fieldLines.ToArray());

		scriptString = scriptString.Replace("#ENTITYFIELDS#", fieldsString);
		return scriptString;
	}

	static List<string> GetEntityFieldsFromExcel(string excelPath)
	{
		var fieldLines = new List<string>();
		var addedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		using (FileStream stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			IWorkbook book = null;
			if (string.Equals(Path.GetExtension(excelPath), ".xls", StringComparison.OrdinalIgnoreCase)) 
				book = new HSSFWorkbook(stream);
			else 
				book = new XSSFWorkbook(stream);

			for (int s = 0; s < book.NumberOfSheets; s++)
			{
				var sheet = book.GetSheetAt(s);
				if (sheet == null || sheet.SheetName.StartsWith("#")) continue;

				var headerRow = sheet.GetRow(0);
				if (headerRow == null) continue;

				var firstDataRow = sheet.GetRow(1);

				for (int c = 0; c < headerRow.LastCellNum; c++)
				{
					var cell = headerRow.GetCell(c);
					if (cell == null || cell.CellType == CellType.Blank) break;

					string colName = cell.CellType == CellType.String ? cell.StringCellValue : cell.ToString();
					if (string.IsNullOrEmpty(colName)) break;
					colName = colName.Trim();

					string safeFieldName = Regex.Replace(colName, @"[^\w]", "_");
					if (string.IsNullOrEmpty(safeFieldName)) continue;
					if (char.IsDigit(safeFieldName[0])) safeFieldName = "_" + safeFieldName;

					if (addedFieldNames.Contains(safeFieldName)) continue;
					addedFieldNames.Add(safeFieldName);

					string fieldType = "string";
					if (firstDataRow != null)
					{
						var dataCell = firstDataRow.GetCell(c);
						if (dataCell != null)
						{
							if (dataCell.CellType == CellType.Boolean) fieldType = "bool";
							else if (dataCell.CellType == CellType.Numeric)
							{
								if (dataCell.NumericCellValue % 1 == 0) fieldType = "int";
								else fieldType = "float";
							}
						}
					}

					fieldLines.Add(string.Format("\t\tpublic {0} {1};", fieldType, safeFieldName));
				}
			}
		}

		if (fieldLines.Count == 0)
		{
			fieldLines.Add("\t\t// public string fieldName;");
		}

		return fieldLines;
	}
}