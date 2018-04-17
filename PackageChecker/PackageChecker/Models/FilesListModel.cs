﻿using PackageChecker.Files;
using PackageChecker.Filtering;
using PackageChecker.WindowManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PackageChecker.Models
{
	internal class FilesListModel : BindableBase, IFilesListManager
	{
		#region Private Properties
		private string _currentFilteringStatus = string.Empty;
		private ObservableCollection<FileRecord> _fileRecords = new ObservableCollection<FileRecord>();

		private IFilteringManager _filteringManager;
		private IProgressBarManager _progressManager;
		private List<FileRecord> _allFileRecords = new List<FileRecord>();

		private const string _filteringStatusTemplate = "Files shown: {0}. Files hidden: {1}.";
		#endregion //Private Properties

		#region Binding Properties
		internal readonly ReadOnlyObservableCollection<FileRecord> FileRecords;

		public string CurrentFilteringStatus
		{
			get
			{
				return _currentFilteringStatus;
			}
			set
			{
				_currentFilteringStatus = value;
				OnPropertyChanged("CurrentFilteringStatus");
			}
		}
		#endregion //Binding Properties

		internal FilesListModel(IFilteringManager filteringManager, IProgressBarManager progressManager)
		{
			FileRecords = new ReadOnlyObservableCollection<FileRecord>(_fileRecords);

			_filteringManager = filteringManager;
			_progressManager = progressManager;

			filteringManager.OnFilteringUpdate += () =>
			{
				ApplyFilteting();
			};

			UpdateFilteringStatus();
		}

		#region Interface implementestion
		public Task UpdateFileRecords(string path, FileSearchType type)
		{
			Task task = new Task(() => UpdateFileRecordsAsync(path, type));
			task.Start();
			return task;
		}

		public void ClearList()
		{
			_allFileRecords.Clear();
			_fileRecords.Clear();
			ApplyFilteting();
		}
		#endregion //Interface implementestion

		#region Private Methods
		private void ApplyFilteting()
		{
			_fileRecords.Clear();
			FilteringInfo info = _filteringManager.GetFilteringInfo();
			foreach (FileRecord record in _allFileRecords)
			{
				if (info.IsCorrectFileRecord(record))
				{
					_fileRecords.Add(record);
				}

				if (info.DoHighlightRecord(record))
				{
					record.DoHighlight = true;
				}
				else
				{
					record.DoHighlight = false;
				}
			}

			UpdateFilteringStatus();
		}

		private void UpdateFileRecordsAsync(string path, FileSearchType type)
		{
			switch (type)
			{
				case FileSearchType.Folder:
					_allFileRecords = GetAllFolderRecords(path);
					break;
				case FileSearchType.Zip:
					_allFileRecords = GetAllZipRecords(path);
					break;
				default:
					throw new ArgumentException(nameof(type));
			}

			DispatcherInvoke(() => ApplyFilteting());
		}

		private List<FileRecord> GetAllFolderRecords(string dirPath)
		{
			List<string> filePaths = DirSearch(dirPath);
			return CollectFilesInfo(filePaths, dirPath);
		}

		private List<FileRecord> GetAllZipRecords(string zipPath)
		{
			string temptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			Directory.CreateDirectory(temptPath);
			try
			{
				ZipExtract(zipPath, temptPath);
				List<string> filePaths = DirSearch(temptPath);
				return CollectFilesInfo(filePaths, temptPath);
			}
			finally
			{
				Directory.Delete(temptPath, true);
			}
		}

		private void ZipExtract(string zipPath, string dirPath)
		{
			UpdateProgressText("Unzipping file");
			IsProgressIndeterminate(false);

			using (ZipArchive zip = ZipFile.OpenRead(zipPath))
			{
				ReadOnlyCollection<ZipArchiveEntry> zipFiles = zip.Entries;

				int currentItem = 0;
				int allItemsCount = zipFiles.Count;

				foreach (ZipArchiveEntry entry in zipFiles)
				{
					if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
						entry.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					{
						string destinationPath = Path.Combine(dirPath, FilesHelper.ReplaseAltSeparators(entry.FullName));

						FileInfo destinationPathFileInfo = new FileInfo(destinationPath);
						if (!destinationPathFileInfo.Directory.Exists)
						{
							Directory.CreateDirectory(destinationPathFileInfo.DirectoryName);
						}

						entry.ExtractToFile(destinationPath, true);
					}

					currentItem++;
					UpdateProgress(100 * currentItem / allItemsCount);
				}
			}
		}

		private List<FileRecord> CollectFilesInfo(List<string> filePaths, string dirPath)
		{
			UpdateProgressText("Collecting files info");
			IsProgressIndeterminate(false);

			List<FileRecord> allFiles = new List<FileRecord>(filePaths.Count);

			int currentItem = 0;
			int allItemsCount = filePaths.Count;

			foreach (string filePath in filePaths)
			{
				FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
				X509Certificate fileCertificate = null;
				try
				{
					fileCertificate = X509Certificate.CreateFromSignedFile(filePath);
				}
				catch (CryptographicException) { }

				allFiles.Add(new FileRecord()
				{
					FilePath = FilesHelper.GetRelativePath(filePath, dirPath),
					FileVersion = fileVersionInfo.FileVersion,
					ProductVersion = fileVersionInfo.ProductVersion,
					Signature = fileCertificate != null ? fileCertificate.Subject : string.Empty,
				});

				currentItem++;
				UpdateProgress(100 * currentItem / allItemsCount);
			}

			return allFiles;
		}

		private List<string> DirSearch(string dirPath)
		{
			UpdateProgressText("Dirrectory search");
			IsProgressIndeterminate(true);

			List<string> files = new List<string>();

			foreach (string filePath in Directory.GetFiles(dirPath, "*.DLL"))
			{
				files.Add(filePath);
			}
			foreach (string filePath in Directory.GetFiles(dirPath, "*.EXE"))
			{
				files.Add(filePath);
			}
			foreach (string childDirPath in Directory.GetDirectories(dirPath))
			{
				files.AddRange(DirSearch(childDirPath));
			}

			return files;
		}

		private void UpdateFilteringStatus()
		{
			int filesShown = _fileRecords.Count;
			int filesTotal = _allFileRecords.Count;
			int filesHidden = filesTotal - filesShown;

			CurrentFilteringStatus = string.Format(CultureInfo.InvariantCulture, _filteringStatusTemplate, filesShown, filesHidden);
		}

		private void UpdateProgress(int progress)
		{
			DispatcherInvoke(() => _progressManager.Progress = progress);
		}

		private void UpdateProgressText(string progressText)
		{
			DispatcherInvoke(() => _progressManager.ProgressText = progressText);
		}

		private void IsProgressIndeterminate(bool isIndeterminate)
		{
			DispatcherInvoke(() => _progressManager.IsIndeterminate = isIndeterminate);
		}

		private void DispatcherInvoke(Action action)
		{
			App.Current.Dispatcher.Invoke(action);
		}
		#endregion //Private Methods
	}
}