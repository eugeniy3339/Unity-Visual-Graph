002

Status: Awaiting Approval

Context:
AI doesnt have instructions how it must work. It can simply kill the project, because it wouldnt know how it need to do its task. Programmers also doesnt have instructions of working with AI, if they arent expirianced they can bring the project down too.
The instructions for AI usage are written down in this document.

Desicion:

The rules from adr 001 are staying the same (where do i need to place file). AI cant pass this limit. Idea, that passes this limit is being thrown away.
Programmer wont promt AI something like, "Complete the game", neither "Do the enemy logic".

Programmer will write down the task and paths to files.

The code AI wrote must have sence. If the code isnt needed/written bad, then its being discarded by programmers. Programmers are only comitting the code they can read and tell what its doing.

AI cant add/change .asmdef, input asset files, neither adr documents. Those files are being changed by people only.

In the pull request description programmers are telling what they accepted from ai code and what they discarted.

Consequences:
AI model can speed up the process of developing, but cant pass the limits we determined.
When we are deciding, either the code line will be merged to the main branch, we are following this document.