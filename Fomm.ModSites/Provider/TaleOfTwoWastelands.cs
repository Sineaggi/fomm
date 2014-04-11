﻿using System;
using System.Collections.Generic;
using GeMod.Interface;

namespace Fomm.ModSites.Provider
{
	/// <summary>
	/// Description of TaleOfTwoWastelands.
	/// </summary>
	public class TaleOfTwoWastelands : IBaseAPI
	{
		private readonly string[] m_strWebsiteURIPatterns = {
			@"taleoftwowastelands\.com"
		};

    public bool IsSupported(IFomodInfo p_modInfo)
    {
      return true;
    }

    public ModVersion GetLatestVersion(IFomodInfo p_modInfo)
    {
      return null;
    }
	}
}
