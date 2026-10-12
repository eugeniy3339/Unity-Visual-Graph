using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

[InitializeOnLoad]
public class ExcelImporter : AssetPostprocessor
{
	class ExcelAssetInfo
	{
		public Type AssetType { get; set; }
		public ExcelAssetAttribute Attribute { get; set; } 
		public string ExcelName
		{
			get
			{
				return string.IsNullOrEmpty(Attribute.ExcelName) ? AssetType.Name : Attribute.ExcelName;
			}
		}
	}

	static List<ExcelAssetInfo> cachedInfos = null; // Clear on compile.

	static ExcelImporter()
	{
		EditorApplication.delayCall += CheckPendingImports;
	}

	static void CheckPendingImports()
	{
		string pending = EditorPrefs.GetString("ExcelImporter_PendingList", "");
		if (string.IsNullOrEmpty(pending)) return;

		EditorPrefs.DeleteKey("ExcelImporter_PendingList");
		var paths = pending.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var path in paths)
		{
			if (File.Exists(path))
			{
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
		}
	}

	static void RegisterPendingImport(string excelPath)
	{
		string pending = EditorPrefs.GetString("ExcelImporter_PendingList", "");
		if (!pending.Contains(excelPath))
		{
			if (!string.IsNullOrEmpty(pending)) pending += ";";
			pending += excelPath;
			EditorPrefs.SetString("ExcelImporter_PendingList", pending);
		}
	}

	static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		bool imported = false;
		foreach(string path in importedAssets)
		{
			string ext = Path.GetExtension(path);
			if(ext.Equals(".xls", StringComparison.OrdinalIgnoreCase) || ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) 
			{
				var excelName = Path.GetFileNameWithoutExtension(path);
				if(excelName.StartsWith("~$")) continue;

				cachedInfos = FindExcelAssetInfos();
				ExcelAssetInfo info = cachedInfos.Find(i => string.Equals(i.ExcelName, excelName, StringComparison.OrdinalIgnoreCase));

				if(info == null)
				{
					// If no ScriptableObject class exists with [ExcelAsset], check if a script file exists anywhere
					if (!ScriptOrClassExists(excelName))
					{
						string scriptPath = Path.ChangeExtension(path, ".cs");
						ExcelAssetScriptMenu.CreateScriptFromExcel(path, scriptPath);
						RegisterPendingImport(path);
						AssetDatabase.ImportAsset(scriptPath);
					}
					continue;
				}

				ImportExcel(path, info);
				imported = true;
			}
		}

		if(imported) 
		{
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	static bool ScriptOrClassExists(string excelName)
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				foreach (var type in assembly.GetTypes())
				{
					if (string.Equals(type.Name, excelName, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			catch {}
		}

		string[] matchingFiles = Directory.GetFiles(Application.dataPath, excelName + ".cs", SearchOption.AllDirectories);
		if (matchingFiles.Length > 0)
		{
			return true;
		}

		return false;
	}

	static List<ExcelAssetInfo> FindExcelAssetInfos()
	{
		var list = new List<ExcelAssetInfo>();
		foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				foreach(var type in assembly.GetTypes())
				{
					var attributes = type.GetCustomAttributes(typeof(ExcelAssetAttribute), false);
					if(attributes.Length == 0) continue;
					var attribute = (ExcelAssetAttribute)attributes[0];
					var info = new ExcelAssetInfo()
					{
						AssetType = type,
						Attribute = attribute
					};
					list.Add(info);
				}
			}
			catch {}
		}
		return list;
	}

	static UnityEngine.Object LoadOrCreateAsset(string assetPath, Type assetType)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

		var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);

		if (asset == null)
		{
			asset = ScriptableObject.CreateInstance(assetType);
			AssetDatabase.CreateAsset((ScriptableObject)asset, assetPath);
			asset.hideFlags = HideFlags.NotEditable;
		}

		return asset;
	}

	static IWorkbook LoadBook(string excelPath)
	{
		using(FileStream stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			if (string.Equals(Path.GetExtension(excelPath), ".xls", StringComparison.OrdinalIgnoreCase)) 
				return new HSSFWorkbook(stream);
			else 
				return new XSSFWorkbook(stream);
		}
	}

	static List<string> GetFieldNamesFromSheetHeader(ISheet sheet)
	{
		var fieldNames = new List<string>();
		if (sheet == null) return fieldNames;

		IRow headerRow = sheet.GetRow(0);
		if (headerRow == null) return fieldNames;

		for (int i = 0; i < headerRow.LastCellNum; i++)
		{
			var cell = headerRow.GetCell(i);
			if(cell == null || cell.CellType == CellType.Blank) break;
			string colName = cell.CellType == CellType.String ? cell.StringCellValue : cell.ToString();
			if(string.IsNullOrEmpty(colName)) break;
			fieldNames.Add(colName.Trim());
		}
		return fieldNames;
	}

	static object CellToFieldObject(ICell cell, FieldInfo fieldInfo, bool isFormulaEvaluate = false)
	{
		var type = isFormulaEvaluate ? cell.CachedFormulaResultType : cell.CellType;

		switch(type)
		{
			case CellType.String:
				if (fieldInfo.FieldType.IsEnum) 
					return Enum.Parse(fieldInfo.FieldType, cell.StringCellValue);
				if (fieldInfo.FieldType == typeof(string)) 
					return cell.StringCellValue;
				return Convert.ChangeType(cell.StringCellValue, fieldInfo.FieldType, System.Globalization.CultureInfo.InvariantCulture);

			case CellType.Boolean:
				if (fieldInfo.FieldType == typeof(bool)) 
					return cell.BooleanCellValue;
				return Convert.ChangeType(cell.BooleanCellValue, fieldInfo.FieldType, System.Globalization.CultureInfo.InvariantCulture);

			case CellType.Numeric:
				if (fieldInfo.FieldType == typeof(string))
				{
					if (cell.NumericCellValue % 1 == 0)
						return ((long)cell.NumericCellValue).ToString();
					return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				if (fieldInfo.FieldType.IsEnum)
					return Enum.ToObject(fieldInfo.FieldType, Convert.ToInt32(cell.NumericCellValue));
				return Convert.ChangeType(cell.NumericCellValue, fieldInfo.FieldType, System.Globalization.CultureInfo.InvariantCulture);

			case CellType.Formula:
				if(isFormulaEvaluate) return null;
				return CellToFieldObject(cell, fieldInfo, true); 

			default:
				if(fieldInfo.FieldType.IsValueType)
				{
					return Activator.CreateInstance(fieldInfo.FieldType);
				}
				return null;
		}
	}

	static object CreateEntityFromRow(IRow row, List<string> columnNames, Type entityType, string sheetName)
	{
		var entity = Activator.CreateInstance(entityType);

		for (int i = 0; i < columnNames.Count; i++)
		{
			FieldInfo entityField = entityType.GetField(
				columnNames[i],
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase
			);
			if (entityField == null) continue;
			if (!entityField.IsPublic && entityField.GetCustomAttributes(typeof(SerializeField), false).Length == 0) continue;

			ICell cell = row.GetCell(i);
			if (cell == null) continue;

			try
			{
				object fieldValue = CellToFieldObject(cell, entityField);
				entityField.SetValue(entity, fieldValue);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Invalid excel cell type at row {0}, column {1}, field '{2}' in sheet '{3}'. Error: {4}", 
					row.RowNum, cell.ColumnIndex, columnNames[i], sheetName, e.Message));
			}
		}
		return entity;
	}

	static object GetEntityListFromSheet(ISheet sheet, Type entityType)
	{
		Type listType = typeof(List<>).MakeGenericType(entityType);
		MethodInfo listAddMethod = listType.GetMethod("Add", new Type[]{entityType});
		object list = Activator.CreateInstance(listType);

		if (sheet == null) return list;

		List<string> excelColumnNames = GetFieldNamesFromSheetHeader(sheet);
		if (excelColumnNames.Count == 0) return list;

		// row of index 0 is header
		for (int i = 1; i <= sheet.LastRowNum; i++)
		{
			IRow row = sheet.GetRow(i);
			if(row == null) continue;

			ICell entryCell = row.GetCell(0); 
			if(entryCell == null || entryCell.CellType == CellType.Blank) continue;

			// skip comment row
			if(entryCell.CellType == CellType.String && entryCell.StringCellValue.StartsWith("#")) continue;

			var entity = CreateEntityFromRow(row, excelColumnNames, entityType, sheet.SheetName);
			listAddMethod.Invoke(list, new object[] { entity });
		}
		return list;
	}

	static bool IsExcelTabType(Type type, out Type entityType)
	{
		entityType = null;
		if (type == null) return false;

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExcelTab<>))
		{
			entityType = type.GetGenericArguments()[0];
			return true;
		}

		Type baseType = type.BaseType;
		while (baseType != null && baseType != typeof(object))
		{
			if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ExcelTab<>))
			{
				entityType = baseType.GetGenericArguments()[0];
				return true;
			}
			baseType = baseType.BaseType;
		}

		return false;
	}

	static void SetTabName(object tabInstance, Type tabType, string name)
	{
		if (tabInstance == null) return;
		var field = tabType.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
		if (field != null)
		{
			field.SetValue(tabInstance, name);
			return;
		}
		var prop = tabType.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
		if (prop != null && prop.CanWrite)
		{
			prop.SetValue(tabInstance, name, null);
		}
	}

	static void SetTabClasses(object tabInstance, Type tabType, object classes)
	{
		if (tabInstance == null) return;
		var field = tabType.GetField("classes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
		if (field != null)
		{
			field.SetValue(tabInstance, classes);
			return;
		}
		var prop = tabType.GetProperty("classes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
		if (prop != null && prop.CanWrite)
		{
			prop.SetValue(tabInstance, classes, null);
		}
	}

	static void ImportExcel(string excelPath, ExcelAssetInfo info)
	{
		string assetPath = "";
		string assetName = info.AssetType.Name + ".asset";

		if(string.IsNullOrEmpty(info.Attribute.AssetPath))
		{
			string basePath = Path.GetDirectoryName(excelPath);
			assetPath = Path.Combine(basePath, assetName);
		}else{
			var path = Path.Combine("Assets", info.Attribute.AssetPath);
			assetPath = Path.Combine(path, assetName);
		}
		UnityEngine.Object asset = LoadOrCreateAsset(assetPath, info.AssetType);

		IWorkbook book = LoadBook(excelPath);

		var assetFields = info.AssetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		int sheetCount = 0;

		foreach (var assetField in assetFields)
		{
			if (!assetField.IsPublic && assetField.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
				continue;

			Type fieldType = assetField.FieldType;

			// 1. List<ExcelTab<T>> or IList<ExcelTab<T>>
			if (fieldType.IsGenericType && (fieldType.GetGenericTypeDefinition() == typeof(List<>) || fieldType.GetGenericTypeDefinition() == typeof(IList<>)))
			{
				Type itemType = fieldType.GetGenericArguments()[0];
				if (IsExcelTabType(itemType, out Type entityType))
				{
					Type listType = typeof(List<>).MakeGenericType(itemType);
					IList tabList = (IList)Activator.CreateInstance(listType);

					for (int i = 0; i < book.NumberOfSheets; i++)
					{
						ISheet sheet = book.GetSheetAt(i);
						if (sheet == null || sheet.SheetName.StartsWith("#")) continue;

						object tabInstance = Activator.CreateInstance(itemType);
						SetTabName(tabInstance, itemType, sheet.SheetName);

						object entities = GetEntityListFromSheet(sheet, entityType);
						SetTabClasses(tabInstance, itemType, entities);

						tabList.Add(tabInstance);
						sheetCount++;
					}

					assetField.SetValue(asset, tabList);
					continue;
				}
			}

			// 2. ExcelTab<T>[] array
			if (fieldType.IsArray)
			{
				Type itemType = fieldType.GetElementType();
				if (itemType != null && IsExcelTabType(itemType, out Type entityType))
				{
					var validSheets = new List<ISheet>();
					for (int i = 0; i < book.NumberOfSheets; i++)
					{
						ISheet sheet = book.GetSheetAt(i);
						if (sheet != null && !sheet.SheetName.StartsWith("#"))
							validSheets.Add(sheet);
					}

					Array tabArr = Array.CreateInstance(itemType, validSheets.Count);
					for (int i = 0; i < validSheets.Count; i++)
					{
						ISheet sheet = validSheets[i];
						object tabInstance = Activator.CreateInstance(itemType);
						SetTabName(tabInstance, itemType, sheet.SheetName);

						object entities = GetEntityListFromSheet(sheet, entityType);
						SetTabClasses(tabInstance, itemType, entities);

						tabArr.SetValue(tabInstance, i);
						sheetCount++;
					}

					assetField.SetValue(asset, tabArr);
					continue;
				}
			}

			// 3. Single ExcelTab<T> whose field name matches a sheet name
			if (IsExcelTabType(fieldType, out Type tabEntityType))
			{
				ISheet sheet = book.GetSheet(assetField.Name);
				if (sheet != null)
				{
					object tabInstance = Activator.CreateInstance(fieldType);
					SetTabName(tabInstance, fieldType, sheet.SheetName);

					object entities = GetEntityListFromSheet(sheet, tabEntityType);
					SetTabClasses(tabInstance, fieldType, entities);

					assetField.SetValue(asset, tabInstance);
					sheetCount++;
					continue;
				}
			}

			// 4. Original List<T> whose field name matches a sheet name
			ISheet sheetNamed = book.GetSheet(assetField.Name);
			if (sheetNamed != null)
			{
				if (fieldType.IsGenericType && (fieldType.GetGenericTypeDefinition() == typeof(List<>) || fieldType.GetGenericTypeDefinition() == typeof(IList<>)))
				{
					Type entityType = fieldType.GetGenericArguments()[0];
					object entities = GetEntityListFromSheet(sheetNamed, entityType);
					assetField.SetValue(asset, entities);
					sheetCount++;
					continue;
				}
			}
		}

		if(info.Attribute.LogOnImport)
		{
			Debug.Log(string.Format("Imported {0} sheets from {1}.", sheetCount, excelPath));
		}

		EditorUtility.SetDirty(asset);
	}
}
