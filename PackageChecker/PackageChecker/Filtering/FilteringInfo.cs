﻿using PackageChecker.Files;

namespace PackageChecker.Filtering
{
	public class FilteringInfo
	{
		internal const string notSymbol = "!";
		internal const string specialSymbol = "*";

		public FilteringCondition ProductVersionCondition { get; private set; }
		public FilteringCondition FileVersionCondition { get; private set; }
		public FilteringCondition FilePathCondition { get; private set; }
		public FilteringCondition SignatureCondition { get; private set; }

		public FilteringInfo()
		{
			ProductVersionCondition = new FilteringCondition();
			FileVersionCondition = new FilteringCondition();
			FilePathCondition = new FilteringCondition();
			SignatureCondition = new FilteringCondition();
		}

		public bool IsCorrectFileRecord(FileRecord record)
		{
			return IsValuePassCondition(ProductVersionCondition, record.ProductVersion) &&
				IsValuePassCondition(FileVersionCondition, record.FileVersion) &&
				IsValuePassCondition(FilePathCondition, record.FilePath) &&
				IsValuePassCondition(SignatureCondition, record.Signature);
		}

		public bool DoHighlightRecord(FileRecord record)
		{
			return IsHighlightCondition(ProductVersionCondition, record.ProductVersion) ||
				IsHighlightCondition(FileVersionCondition, record.FileVersion) ||
				IsHighlightCondition(FilePathCondition, record.FilePath) ||
				IsHighlightCondition(SignatureCondition, record.Signature);
		}

		private bool IsValuePassCondition(FilteringCondition condition, string value)
		{
			foreach (string filter in condition.EntityInclude)
			{
				if (!ContainsWithPattern(value, filter))
				{
					return false;
				}
			}

			return true;
		}

		private bool IsHighlightCondition(FilteringCondition condition, string value)
		{
			foreach (string filter in condition.EntityHighlignt)
			{
				if (ContainsWithPattern(value, filter))
				{
					return true;
				}
			}

			return false;
		}

		private bool ContainsWithPattern(string source, string value)
		{
			bool invertResult = false;
			bool isStartsWith = false;
			bool isEndsWith = false;
			string localValue = value;

			if (localValue.StartsWith(notSymbol))
			{
				invertResult = true;
				localValue = localValue.Substring(1, localValue.Length - 1);
			}

			if (string.IsNullOrEmpty(localValue))
			{
				return string.IsNullOrEmpty(source) ^ invertResult;
			}

			if (localValue.StartsWith(specialSymbol))
			{
				isEndsWith = true;
				localValue = localValue.Substring(1, localValue.Length - 1);
			}

			if (localValue.EndsWith(specialSymbol))
			{
				isStartsWith = true;
				localValue = localValue.Substring(0, localValue.Length - 1);
			}

			if (isStartsWith && isEndsWith)
			{
				return source.Contains(localValue) ^ invertResult;
			}
			else if (isStartsWith)
			{
				return source.StartsWith(localValue) ^ invertResult;
			}
			else if (isEndsWith)
			{
				return source.EndsWith(localValue) ^ invertResult;
			}

			return source == localValue ^ invertResult;
		}
	}
}