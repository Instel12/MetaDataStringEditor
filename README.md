# USE THIS SHART RESPONCIVLY AND DONT USE IT TO CHEAT IN GAMES PLEASE
(well I guess you **COULD** but I do not condome it)
# Partial String Modification Tool for global-metadata.dat
  For Android games exported with Unity’s IL2CPP scripting backend, the strings used in the code are compiled into the assets\bin\Data\Managed\Metadata\global-metadata.dat file. As part of localization work, I made a simple tool to modify some of the strings inside this file.

# References
[il2cppdumper](https://github.com/Perfare/Il2CppDumper)
My understanding of this file’s structure mainly comes from studying the source code of this tool. This tool is originally designed to export class definitions from compiled libil2cpp.so files and global-metadata.dat, producing outputs like IDA renaming scripts, DLLs usable by UABE and AssetStudio, etc. It’s a very useful tool.

# What is Modified
  In global-metadata.dat, the strings used in code are stored with a header list containing each string’s offset, length, and other info, followed by a data section where all strings are packed closely together. Because there is a header list, the strings don’t need to be null-terminated (\0).
  Since the number of strings stays the same before and after modification, the list is updated by directly overwriting the original area. The length of the data section may change; if the modified data section is shorter or equal in length to the original, it is overwritten in place. If it becomes longer, it is written at the end of the file.
