using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CraftSynth.BuildingBlocks.Regenerator
{
	internal class Regenerator
	{
		#region Private Members
		private const string CraftsynthBuildingBlocksFolderPathVariableName = "CRAFTSYNTH_BUILDING_BLOCKS_FOLDER_PATH";
		private bool _shouldPerform;
		private static readonly List<string> extensionsToConsider = new List<string>() { ".resx", ".Designer.cs", ".cs" };
		private static readonly string fileNamePrefixToConsider =  "CraftSynth.BuildingBlocks.";

		private string csProjFilePath;
		private string csProjFilePathInOriginalBB;
		private List<IncludedFile> IncludedItems;
		private List<IncludedFile> IncludedItemsInOriginalBB;

		private List<string> filesRequiredForMinimalVersion; 
		#endregion

		#region Properties

		#endregion

		#region Public Methods

		public void BuildMinimalVersion()
		{
			try
			{
				if (!this._shouldPerform)
				{
					LogError(new Exception("CRAFTSYNTH_BUILDING_BLOCKS_FOLDER_PATH not found in Windows Environment Variables. This is OK if you are not developing BuildingBlocks."));
				}
				else 
				{
					//get files that are required from .ini 
					this.filesRequiredForMinimalVersion = new List<string>();
					if (GetVersionFromIniFile() >= 3)
					{
						this.filesRequiredForMinimalVersion = this.GetFileNamesFromSectionInIniFile("FilesRequiredForMinimalVersion");
					}
					else
					{
						var lines = File.ReadAllLines(Path.Combine(ApplicationPhysicalPath, "Regenerator.ini"));
						foreach (string line in lines)
						{
							if (!string.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("["))
							{
								this.filesRequiredForMinimalVersion.Add(line.Trim());
							}
						}
					}

					//check existance of each in original BB project
					foreach (string f in this.filesRequiredForMinimalVersion)
					{
						if (
							!this.IncludedItemsInOriginalBB.Exists(
								i => i.IsJustLink == false && string.Compare(i.FileName, f.Trim(), StringComparison.OrdinalIgnoreCase) == 0))
						{
							throw new Exception(
								string.Format(
									"File '{0}' is specified as required in .ini but it is not included in original BB project '{1}' or file is specified in [FilesNeverToInclude] section in ini.",
									f.Trim(), this.csProjFilePathInOriginalBB));
						}
					}


					//first exclude all non-required files that have one of considered extensions
					for (int j = this.IncludedItems.Count - 1; j >= 0; j--)
					{
						IncludedFile includedFile = this.IncludedItems[j];
						if (filesRequiredForMinimalVersion.Exists(f => string.Compare(f, includedFile.FileName, StringComparison.OrdinalIgnoreCase) == 0))
						{//this is required file and already included
							if (!includedFile.IsJustLink)
							{
								string originalFilePath = Path.Combine(Path.GetDirectoryName(this.csProjFilePathInOriginalBB),
									includedFile.FileName);
								if (!File.Exists(includedFile.FilePath) || !FilesContentsAreEqual(includedFile.FilePath, originalFilePath))
								{//this is real file but has different content -overwrite it with original
									File.Copy(originalFilePath, includedFile.FilePath, true);
								}
								else
								{//this is real file and is same as original -that is what we need -do nothing

								}
							}
							else
							{//this is link to file -remove it
								if (!ExcludeFromProject(csProjFilePath, includedFile.FileName))
								{
									ExcludeFromProject(csProjFilePath, includedFile.FilePath);
								}
								this.IncludedItems.RemoveAt(j);
							}
						}
						else
						{
							if (!extensionsToConsider.Exists(e => includedFile.FileName.ToLower().EndsWith(e.ToLower())) || !includedFile.FileName.ToLower().StartsWith(fileNamePrefixToConsider.ToLower()))
							{//this is non-required file but also has strange extension
								//leave it
							}
							else
							{//this is non-required file and has one of extensions we consider - exclude it
								if (!this.ExcludeFromProject(this.csProjFilePath, includedFile.FileName))
								{
									this.ExcludeFromProject(this.csProjFilePath, includedFile.FilePath);
								}
								this.IncludedItems.RemoveAt(j);
							}
						}
					}

					//then delete  all non-reqired files that have one of considered extensions
					var localFilePaths = GetFilePaths(ApplicationPhysicalPath, extensionsToConsider, fileNamePrefixToConsider);
					for (int i = localFilePaths.Count - 1; i >= 0; i--)
					{
						if (this.filesRequiredForMinimalVersion.Exists(r => string.Compare(Path.GetFileName(localFilePaths[i]), r.Trim(), StringComparison.OrdinalIgnoreCase) == 0))
						{//this local file is required -do nothing

						}
						else
						{//this local file is not required - delete it
							File.Delete(localFilePaths[i]);
						}
					}

					//lastly insure that each required file exist and is same as original one or perform copy from original file
					//and also insure it is included in project
					foreach (string requiredFile in filesRequiredForMinimalVersion)
					{
						var fileAtOriginalBB =this.IncludedItemsInOriginalBB.SingleOrDefault(i => i.IsJustLink == false && string.Compare(i.FileName, requiredFile, StringComparison.OrdinalIgnoreCase) == 0);
						String filePathLocal = Path.Combine(ApplicationPhysicalPath, fileAtOriginalBB.FileName);

						if (
							!this.IncludedItems.Exists(
								i => i.IsJustLink == false && string.Compare(i.FileName, requiredFile, StringComparison.OrdinalIgnoreCase) == 0))
						{//not included
							File.Copy(fileAtOriginalBB.FilePath, filePathLocal, true);
							IncludeInProject(this.csProjFilePath, fileAtOriginalBB.FileName, false);
							this.IncludedItems.Add(new IncludedFile(fileAtOriginalBB.FileName, filePathLocal, false));
						}
						else
						{//included allready
							if (File.Exists(filePathLocal))
							{
								if (FilesContentsAreEqual(filePathLocal, fileAtOriginalBB.FilePath))
								{
									//do nothing
								}
								else
								{
									File.Copy(fileAtOriginalBB.FilePath, filePathLocal, true);
								}
							}
						}
					}

					ClearErrorLog();
				}
			}
			catch (Exception e)
			{
				LogError(e);
			}
		}

		public void LinkFullVersion()
		{
			try
			{
				if (!this._shouldPerform)
				{
					LogError(new Exception("CRAFTSYNTH_BUILDING_BLOCKS_FOLDER_PATH not found in Windows Environment Variables. This is OK if you are not developing BuildingBlocks."));
				}
				else
				{
					//first exclude all files that has one of extension we consider but are not part of original BB
					for (int j = this.IncludedItems.Count - 1; j >= 0; j--)
					{
						IncludedFile includedFile = this.IncludedItems[j];
						if (this.IncludedItemsInOriginalBB.Exists(f => string.Compare(f.FilePath, includedFile.FilePath, StringComparison.OrdinalIgnoreCase) == 0))
						{//this is required file and already included
							if (includedFile.IsJustLink)
							{
								//and is just link -that is what we need -do nothing
							}
							else
							{//and is not link - reinclude as link
								if (!ExcludeFromProject(csProjFilePath, includedFile.FilePath))
								{
									ExcludeFromProject(csProjFilePath, includedFile.FileName);
								}
								IncludeInProject(csProjFilePath, includedFile.FilePath, true);
								includedFile.IsJustLink = true;
							}
						}
						else
						{
							if (!extensionsToConsider.Exists(e => includedFile.FileName.ToLower().EndsWith(e.ToLower())) || !includedFile.FileName.ToLower().StartsWith(fileNamePrefixToConsider.ToLower()))
							{//this is non-required file but also has strange extension
								//leave it
							}
							else
							{//this is non-required file and has one of extensions we consider - exclude it
								if (!this.ExcludeFromProject(this.csProjFilePath, includedFile.FilePath))
								{
									this.ExcludeFromProject(this.csProjFilePath, includedFile.FileName);
								}
								this.IncludedItems.RemoveAt(j);
							}
						}
					}

					//second insure that all files included in original BB are linked
					foreach (IncludedFile includedFileInOriginalBB in IncludedItemsInOriginalBB)
					{
						if (!this.IncludedItems.Exists(
							ii => string.Compare(ii.FilePath, includedFileInOriginalBB.FilePath, StringComparison.OrdinalIgnoreCase) == 0 &&
							      ii.IsJustLink == true)
							)
						{
							this.IncludeInProject(this.csProjFilePath,
								Path.Combine(Path.GetDirectoryName(includedFileInOriginalBB.FilePath), includedFileInOriginalBB.FileName), true);
						}
					}
					
					ClearErrorLog();
				}
				
			}
			catch (Exception e)
			{
				LogError(e);
			}
		}
		#endregion

		#region Constructors And Initialization

		public Regenerator()
		{
			string folderPathOfBuildingBlocks = Environment.GetEnvironmentVariable(Regenerator.CraftsynthBuildingBlocksFolderPathVariableName, EnvironmentVariableTarget.User);
			this._shouldPerform = !string.IsNullOrEmpty(folderPathOfBuildingBlocks);

			if (_shouldPerform)
			{
				this.csProjFilePathInOriginalBB = Path.Combine(folderPathOfBuildingBlocks, "CraftSynth.BuildingBlocks.csproj");
				this.csProjFilePath = GetFilePaths(ApplicationPhysicalPath,"*.csproj")[0];

				this.IncludedItems = this.GetAllFilesIncludedInProject(this.csProjFilePath, extensionsToConsider, fileNamePrefixToConsider);
				this.IncludedItemsInOriginalBB = this.GetAllFilesIncludedInProject(this.csProjFilePathInOriginalBB, extensionsToConsider, fileNamePrefixToConsider);

				if (GetVersionFromIniFile() >= 3)
				{
					var filesNeverToInclude = GetFileNamesFromSectionInIniFile("FilesNeverToInclude");
					if (filesNeverToInclude != null && filesNeverToInclude.Count > 0)
					{
						foreach (string fileNeverToInclude in filesNeverToInclude)
						{
							this.IncludedItemsInOriginalBB.RemoveAll(item => string.Compare(item.FileName, fileNeverToInclude, StringComparison.OrdinalIgnoreCase) == 0);
						}
					}
				}
			}
		}
		#endregion

		#region Deinitialization And Destructors

		#endregion

		#region Event Handlers

		#endregion

		#region Private Methods
		private List<IncludedFile> GetAllFilesIncludedInProject(string csProjPath, List<string> extensionsToConsider, string fileNamePrefixToConsider)
		{
			List<IncludedFile> r = new List<IncludedFile>();

			XmlDocument doc = new XmlDocument();
			doc.Load(csProjPath);
			XmlNamespaceManager manager = new XmlNamespaceManager(doc.NameTable);
			manager.AddNamespace("ns", doc.DocumentElement.Attributes["xmlns"].Value);
			XmlNodeList list = doc.SelectNodes("//ns:Compile", manager);
			foreach (XmlNode node in list)
			{
				if (node.Attributes["Include"] != null)
				{

					var newItem = new IncludedFile();
					newItem.IsJustLink = node.HasChildNodes && node.ChildNodes[0].Name == "Link";
					if (newItem.IsJustLink)
					{
						newItem.FilePath = node.Attributes["Include"].Value;
						newItem.FileName = Path.GetFileName(newItem.FilePath);
					}
					else
					{
						newItem.FileName = node.Attributes["Include"].Value;
						newItem.FilePath = Path.Combine(Path.GetDirectoryName(csProjPath), newItem.FileName);
					}
					
					if (
						string.Compare(Path.GetDirectoryName(newItem.FilePath), Path.GetDirectoryName(this.csProjFilePath), StringComparison.OrdinalIgnoreCase) != 0
						&& string.Compare(Path.GetDirectoryName(newItem.FilePath), Path.GetDirectoryName(this.csProjFilePathInOriginalBB), StringComparison.OrdinalIgnoreCase) != 0
						)
					{
						//this file is not is local folder nor BB folder -skip
					}else
					{
						bool matchExtension = false;
						foreach (string ext in extensionsToConsider)
						{
							if (newItem.FileName.ToLower().EndsWith(ext.ToLower()) && newItem.FileName.ToLower().StartsWith(fileNamePrefixToConsider.ToLower()))
							{
								matchExtension = true;
							}
						}

						if (matchExtension)
						{
							r.Add(newItem);
						}
					}
				}
			}

			return r;
		}
		private bool ExcludeFromProject(string csProjFilePath, string fileNameOrInCaseOfLinkFilePath)
		{
			bool found = false;
			XmlDocument doc = new XmlDocument();
			doc.Load(csProjFilePath);
			XmlNamespaceManager manager = new XmlNamespaceManager(doc.NameTable);
			manager.AddNamespace("ns", doc.DocumentElement.Attributes["xmlns"].Value);
			XmlNodeList list = doc.SelectNodes("//ns:Compile", manager);
			foreach (XmlNode node in list)
			{
				if (node.Attributes["Include"] != null && string.Compare(node.Attributes["Include"].Value, fileNameOrInCaseOfLinkFilePath, StringComparison.OrdinalIgnoreCase) == 0)
				{
					found = true;
					var pn = node.ParentNode;
					pn.RemoveChild(node);
					if (!pn.HasChildNodes && pn.Attributes.Count == 0)
					{
						pn.ParentNode.RemoveChild(pn);
					}
					
				}
			}

			if (found)
			{
				doc.Save(csProjFilePath);
			}

			return found;
		}

		private bool FilesContentsAreEqual(string filePath1, string filePath2)
		{
			bool r;
			FileInfo fi1 = new FileInfo(filePath1);
			FileInfo fi2 = new FileInfo(filePath2);
			if (fi1.Length != fi2.Length)
			{
				r = false;
			}
			else
			{
				using (StreamReader sr1 = fi1.OpenText())
				{
					using (StreamReader sr2 = fi2.OpenText())
					{
						string s1 = sr1.ReadToEnd();
						string s2 = sr2.ReadToEnd();
						r = s1 == s2;
					}
				}
			}

			return r;
		}

		private bool IsIncluded(string csProjFilePath, string fileName)
		{
			bool exists = false;

			XmlDocument doc = new XmlDocument();
			doc.Load(csProjFilePath);
			XmlNamespaceManager manager = new XmlNamespaceManager(doc.NameTable);
			manager.AddNamespace("ns", doc.DocumentElement.Attributes["xmlns"].Value);
			XmlNodeList list = doc.SelectNodes("//ns:Compile", manager);
			foreach (XmlNode node in list)
			{
				if (node.Attributes["Include"] != null && string.Compare(node.Attributes["Include"].Value, fileName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					exists = true;
					break;
				}
			}
			return exists;
		}

		private void IncludeInProject(string csProjFilePath, string fileNameOrInCaseOfLinkFilePath, bool isJustLink = false)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(csProjFilePath);

			//XmlNode newNode = doc.CreateNode(XmlNodeType.Element, "ItemGroup", "ItemGroup");
			//XmlNode newInnerNode = doc.CreateNode(XmlNodeType.Element, "Compile", "Compile");
			//XmlAttribute newAttribute = doc.CreateAttribute("Include",)

			//XmlNode node = doc.SelectSingleNode("Project");

			//node.AppendChild()

			XmlNode ItemGroupNode = doc.CreateNode(XmlNodeType.Element, "ItemGroup", doc.DocumentElement.Attributes["xmlns"].Value);
			XmlNode CompileNode = doc.CreateNode(XmlNodeType.Element, "Compile", doc.DocumentElement.Attributes["xmlns"].Value);
			if (isJustLink)
			{
				XmlNode linkNode = doc.CreateNode(XmlNodeType.Element, "Link", doc.DocumentElement.Attributes["xmlns"].Value);
				linkNode.InnerText = Path.GetFileName(fileNameOrInCaseOfLinkFilePath);
				CompileNode.AppendChild(linkNode);
			}
			XmlAttribute IncludeAttr = doc.CreateAttribute("Include");
			IncludeAttr.Value = fileNameOrInCaseOfLinkFilePath;
			CompileNode.Attributes.Append(IncludeAttr);
			ItemGroupNode.AppendChild(CompileNode);
			doc.DocumentElement.AppendChild(ItemGroupNode);

			doc.Save(csProjFilePath);
		}

		private int GetVersionFromIniFile()
		{
			int r = 1;

			string iniFilePath = Path.Combine(ApplicationPhysicalPath, "Regenerator.ini");
			var lines = File.ReadAllLines(iniFilePath);
			foreach (string line in lines)
			{
				if (!string.IsNullOrEmpty(line))
				{
					if (line.Trim().StartsWith("[Version]="))
					{
						r = int.Parse(line.Trim().Replace("[Version]=", string.Empty));
					}
					else if (line.Trim().StartsWith("[version]="))
					{
						r = int.Parse(line.Trim().Replace("[version]=", string.Empty));
					}
				}
			}

			return r;
		}

		private List<string> GetFileNamesFromSectionInIniFile(string sectionName)
		{
			List<string> r = new List<string>();

			string currentSection = null;
			string iniFilePath = Path.Combine(ApplicationPhysicalPath, "Regenerator.ini");
			var lines = File.ReadAllLines(iniFilePath);
			foreach (string line in lines)
			{
				if (!string.IsNullOrEmpty(line) && !line.Trim().StartsWith("//"))
				{
					if (line.Trim().StartsWith("["))
					{
						currentSection = line.Trim().TrimStart('[').TrimEnd(']');
					}
					else
					{
						if (string.Compare(currentSection, sectionName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							r.Add(line.Trim());
						}
					}
				}
			
			}

			return r;
		} 
		#endregion

		#region Helpers
		/// <summary>
		/// Example: C:\App1
		/// </summary>
		public static string ApplicationPhysicalPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase).Replace("file:\\", String.Empty);
			}

		}

		/// <summary>
		/// Gets list of strings where each is full path to file including filename (for example: <example>c:\dir\filename.ext</example>.
		/// </summary>
		/// <param name="folder">Full path of folder that should be searched. For example: <example>c:\dir</example>.</param>
		/// <param name="searchPatern">Filter that should be used. For example: <example>*.txt</example></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Thrown when parameter is null or empty.</exception>
		public static List<string> GetFilePaths(string folderPath, string searchPatern)
		{
			if (String.IsNullOrEmpty(folderPath)) throw new ArgumentException("Value must be non-empty string.", "folderPath");
			if (String.IsNullOrEmpty(searchPatern)) throw new ArgumentException("Value must be non-empty string.", "searchPatern");

			List<string> filePaths = new List<string>();
			string[] filePathStrings = Directory.GetFiles(folderPath, searchPatern, SearchOption.TopDirectoryOnly);
			if (filePathStrings != null)
			{
				filePaths.AddRange(filePathStrings);
			}

			return filePaths;
		}

		private static List<string> GetFilePaths(string folderPath, List<string> extensionsToConsider, string fileNamePrefixToConsider)
		{
			List<string> localFilePaths = new List<string>();
			foreach (string extension in extensionsToConsider)
			{
				List<string> sourceFiles = GetFilePaths(folderPath,fileNamePrefixToConsider+ "*" + extension);
				foreach (string sourceFile in sourceFiles)
				{
					if (!localFilePaths.Contains(sourceFile.ToLower()))
					{
						localFilePaths.Add(sourceFile.ToLower());
					}
				}
			}
			return localFilePaths;
		}

		private static void LogError(Exception e)
		{
			File.WriteAllText(Path.Combine(ApplicationPhysicalPath,"Regenerator.LastError.txt"), e.Message+"\r\n\r\n"+ e.StackTrace);
		}

		private static void ClearErrorLog()
		{
			string logFilePath = Path.Combine(ApplicationPhysicalPath, "Regenerator.LastError.txt");
			if (File.Exists(logFilePath) && File.ReadAllBytes(logFilePath).Length > 0)
			{
				File.WriteAllText(logFilePath, string.Empty);
			}
		}

		#endregion
	}


		

	

	

		
	

	internal class IncludedFile
	{
		public string FileName;
		public string FilePath;
		public bool IsJustLink;

		public IncludedFile()
		{
		}
		public IncludedFile(string fileName, string filePath, bool isJustLink)
		{
			this.FileName = fileName;
			this.FilePath = filePath;
			this.IsJustLink = isJustLink;
		}
	}

}
