﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

public partial class RemoveDeviceUnknownVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "UnknownDeviceVerificationEnabled",
            table: "User");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "UnknownDeviceVerificationEnabled",
            table: "User",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);
    }
}
