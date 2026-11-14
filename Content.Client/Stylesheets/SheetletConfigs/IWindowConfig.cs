// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IWindowConfig : ISheetletConfig
{
    public ResPath WindowHeaderTexturePath { get; }
    public ResPath WindowHeaderAlertTexturePath { get; }
    public ResPath WindowBackgroundPath { get; }
    public ResPath WindowBackgroundBorderedPath { get; }
    public ResPath TransparentWindowBackgroundBorderedPath { get; }
}
