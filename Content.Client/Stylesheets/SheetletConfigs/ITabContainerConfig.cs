// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ITabContainerConfig : ISheetletConfig
{
    public ResPath TabContainerPanelPath { get; }
}
