// SPDX-FileCopyrightText: 2017-2026 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ICheckboxConfig
{
    public ResPath CheckboxUncheckedPath { get; }
    public ResPath CheckboxCheckedPath { get; }
}

