﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageChecker.WindowManagement
{
	public class FilteringInfo
	{
		private const string specialSymbol = "*";

		public FilteringCondition ProductVersionCondition { get; private set; }
		public FilteringCondition FileVersionCondition { get; private set; }
		public FilteringCondition FilePathCondition { get; private set; }

		public FilteringInfo()
		{
			ProductVersionCondition = new FilteringCondition();
			FileVersionCondition = new FilteringCondition();
			FilePathCondition = new FilteringCondition();
		}

		public bool IsCorrectProductVersion(string productVersion)
		{
			return IsValuePassCondition(ProductVersionCondition, productVersion);
		}

		public bool DoHighlightProductVersion(string productVersion)
		{
			return IsHighlightCondition(ProductVersionCondition, productVersion);
		}

		public bool IsCorrectFileVersion(string fileVersion)
		{
			return IsValuePassCondition(FileVersionCondition, fileVersion);
		}

		public bool DoHighlightFileVersion(string fileVersion)
		{
			return IsHighlightCondition(FileVersionCondition, fileVersion);
		}

		public bool IsCorrectFilePath(string filePath)
		{
			return IsValuePassCondition(FilePathCondition, filePath);
		}

		public bool DoHighlightFilePath(string filePath)
		{
			return IsHighlightCondition(FilePathCondition, filePath);
		}

		private bool IsValuePassCondition(FilteringCondition condition, string value)
		{
			foreach (string filter in condition.EntityEquals)
			{
				if (!ContainsWithPattern(value, filter))
				{
					return false;
				}
			}

			foreach (string filter in condition.EntityNotEquals)
			{
				if (ContainsWithPattern(value, filter))
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
			if (value == string.Empty)
			{
				return string.IsNullOrEmpty(source);
			}

			bool isStartsWith = false;
			bool isEndsWith = false;
			string localValue = value;

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
				return source.Contains(localValue);
			}
			else if (isStartsWith)
			{
				return source.StartsWith(localValue);
			}
			else if (isEndsWith)
			{
				return source.EndsWith(localValue);
			}

			return source == localValue;
		}
	}
}
