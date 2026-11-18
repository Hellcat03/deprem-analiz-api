using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IlkSemasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Binalar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Envanter = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Binalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Depremler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Saglayici = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    KaynakKimligi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Buyukluk = table.Column<double>(type: "float", nullable: false),
                    DerinlikKm = table.Column<double>(type: "float", nullable: false),
                    Bolge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuyuklukTuru = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enlem = table.Column<double>(type: "float", nullable: false),
                    Boylam = table.Column<double>(type: "float", nullable: false),
                    Yer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depremler", x => x.Id);
                    table.CheckConstraint("CK_Deprem_Girdi", "[DerinlikKm] > 0 AND [DerinlikKm] <= 800 AND [Buyukluk] BETWEEN -2 AND 10 AND [Enlem] BETWEEN -90 AND 90 AND [Boylam] BETWEEN -180 AND 180");
                });

            migrationBuilder.CreateTable(
                name: "Izler",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Islem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Izler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incelemeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SonucJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incelemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incelemeler_Binalar_BinaId",
                        column: x => x.BinaId,
                        principalTable: "Binalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hesaplar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepremId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SonucJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hesaplar", x => x.Id);
                    table.UniqueConstraint("AK_Hesaplar_Id_DepremId", x => new { x.Id, x.DepremId });
                    table.ForeignKey(
                        name: "FK_Hesaplar_Depremler_DepremId",
                        column: x => x.DepremId,
                        principalTable: "Depremler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Uyarilar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepremId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HesapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kural = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Seviye = table.Column<int>(type: "int", nullable: false),
                    Zaman = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OnayZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uyarilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uyarilar_Hesaplar_HesapId_DepremId",
                        columns: x => new { x.HesapId, x.DepremId },
                        principalTable: "Hesaplar",
                        principalColumns: new[] { "Id", "DepremId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Depremler_OlusZamani",
                table: "Depremler",
                column: "OlusZamani");

            migrationBuilder.CreateIndex(
                name: "IX_Depremler_Saglayici_KaynakKimligi",
                table: "Depremler",
                columns: new[] { "Saglayici", "KaynakKimligi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hesaplar_DepremId",
                table: "Hesaplar",
                column: "DepremId");

            migrationBuilder.CreateIndex(
                name: "IX_Incelemeler_BinaId",
                table: "Incelemeler",
                column: "BinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Izler_KayitId",
                table: "Izler",
                column: "KayitId");

            migrationBuilder.CreateIndex(
                name: "IX_Uyarilar_DepremId_Kural",
                table: "Uyarilar",
                columns: new[] { "DepremId", "Kural" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uyarilar_HesapId_DepremId",
                table: "Uyarilar",
                columns: new[] { "HesapId", "DepremId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incelemeler");

            migrationBuilder.DropTable(
                name: "Izler");

            migrationBuilder.DropTable(
                name: "Uyarilar");

            migrationBuilder.DropTable(
                name: "Binalar");

            migrationBuilder.DropTable(
                name: "Hesaplar");

            migrationBuilder.DropTable(
                name: "Depremler");
        }
    }
}
