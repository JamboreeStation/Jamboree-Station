// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RemoveRMCPatreon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_patron_lobby_messages");

            migrationBuilder.DropTable(
                name: "rmc_patron_round_end_nt_shoutouts");

            migrationBuilder.DropTable(
                name: "rmc_patrons");

            migrationBuilder.DropTable(
                name: "rmc_patron_tiers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_patron_tiers",
                columns: table => new
                {
                    rmc_patron_tiers_id = table.Column<int>(type: "INTEGER", nullable: false),
                    discord_role = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ghost_color = table.Column<bool>(type: "INTEGER", nullable: false),
                    ghost_cosmetics = table.Column<bool>(type: "INTEGER", nullable: false),
                    ghost_particles = table.Column<bool>(type: "INTEGER", nullable: false),
                    icon = table.Column<string>(type: "TEXT", nullable: true),
                    lobby_message = table.Column<bool>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false),
                    round_end_shoutout = table.Column<bool>(type: "INTEGER", nullable: false),
                    show_on_credits = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_tiers", x => x.rmc_patron_tiers_id);
                });

            migrationBuilder.CreateTable(
                name: "rmc_patrons",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tier_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ghost_color = table.Column<int>(type: "INTEGER", nullable: true),
                    ghost_hat = table.Column<string>(type: "TEXT", nullable: true),
                    ghost_mask = table.Column<string>(type: "TEXT", nullable: true),
                    ghost_particles = table.Column<string>(type: "TEXT", nullable: true),
                    ooc_icon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patrons", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_rmc_patrons_player_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rmc_patrons_rmc_patron_tiers_tier_id",
                        column: x => x.tier_id,
                        principalTable: "rmc_patron_tiers",
                        principalColumn: "rmc_patron_tiers_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_patron_lobby_messages",
                columns: table => new
                {
                    patron_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_lobby_messages", x => x.patron_id);
                    table.ForeignKey(
                        name: "FK_rmc_patron_lobby_messages_rmc_patrons_patron_id",
                        column: x => x.patron_id,
                        principalTable: "rmc_patrons",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_patron_round_end_nt_shoutouts",
                columns: table => new
                {
                    patron_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_round_end_nt_shoutouts", x => x.patron_id);
                    table.ForeignKey(
                        name: "FK_rmc_patron_round_end_nt_shoutouts_rmc_patrons_patron_id",
                        column: x => x.patron_id,
                        principalTable: "rmc_patrons",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rmc_patron_tiers_discord_role",
                table: "rmc_patron_tiers",
                column: "discord_role",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rmc_patron_tiers_lobby_message",
                table: "rmc_patron_tiers",
                column: "lobby_message");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_patron_tiers_round_end_shoutout",
                table: "rmc_patron_tiers",
                column: "round_end_shoutout");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_patrons_tier_id",
                table: "rmc_patrons",
                column: "tier_id");
        }
    }
}
