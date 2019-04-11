﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Core.Utilities
{
    public class Base64Util
	{
		public static string Encode(byte[] input)
		{
			string text = Convert.ToBase64String(input);
			text = text.Split('=')[0];
			text = text.Replace('+', '-');
			return text.Replace('/', '_');
		}

		public static byte[] Decode(string input)
		{
			string text = input.Replace('-', '+');
			text = text.Replace('_', '/');
			switch (text.Length % 4)
			{
				case 2:
					text += "==";
					break;
				case 3:
					text += "=";
					break;
				default:
					throw new ArgumentOutOfRangeException("input", "Illegal base64url string!");
				case 0:
					break;
			}
			return Convert.FromBase64String(text);
		}
	}

}
