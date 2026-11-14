// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IIconConfig : ISheetletConfig
{
    public ResPath HelpIconPath { get; }
    public ResPath CrossIconPath { get; }
    public ResPath RefreshIconPath { get; }
    public ResPath InvertedTriangleIconPath { get; }
}
