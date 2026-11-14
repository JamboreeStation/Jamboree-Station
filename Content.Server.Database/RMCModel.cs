// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("rmc_discord_accounts")]
public sealed class RMCDiscordAccount
{
    [Key]
    public ulong Id { get; set; }

    public RMCLinkedAccount LinkedAccount { get; set; } = default!;
    public List<RMCLinkedAccountLogs> LinkedAccountLogs { get; set; } = default!;
}

[Table("rmc_linked_accounts")]
public sealed class RMCLinkedAccount
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public ulong DiscordId { get; set; }

    public RMCDiscordAccount Discord { get; set; } = default!;
}


[Table("rmc_linking_codes")]
[Index(nameof(Code))]
public sealed class RMCLinkingCodes
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public Guid Code { get; set; }

    public DateTime CreationTime { get; set; }
}

[Table("rmc_linked_accounts_logs")]
[Index(nameof(PlayerId))]
[Index(nameof(DiscordId))]
[Index(nameof(At))]
public sealed class RMCLinkedAccountLogs
{
    [Key]
    public int Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public ulong DiscordId { get; set; }

    public RMCDiscordAccount Discord { get; set; } = default!;

    public DateTime At { get; set; }
}

