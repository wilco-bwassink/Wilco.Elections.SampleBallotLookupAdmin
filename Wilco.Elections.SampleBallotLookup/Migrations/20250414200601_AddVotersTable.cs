using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilco.Elections.SampleBallotLookup.Migrations
{
    /// <inheritdoc />
    public partial class AddVotersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Voters",
                columns: table => new
                {
                    VUID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TOWNID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TNAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NMFIRST = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NMMID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NMLAST = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NMSUFFIX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SREASON = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GENDER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DEFFECTED = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SUPPRESS = table.Column<bool>(type: "bit", nullable: false),
                    SNAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADSTR1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADSTR2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADNUM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADCITY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADST = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADZIP4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADZIP5 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADPREFIX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADSUFFIX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADSTYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADUTYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADPARCEL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ADUNIT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DESIG = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADSTR1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADSTR2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADNUM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADCITY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADST = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MCOUNTRY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADZIP4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADZIP5 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MADUNIT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MUTYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MDESIG = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FL_AD_OVERSEAS = table.Column<bool>(type: "bit", nullable: false),
                    DASSIGNMENT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PRECINCT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    COMM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SBE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STREP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STSEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    USREP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CD_DIST_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NB_DISTRICT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCHOOL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CITY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OTHER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HISPANIC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DTBALLOT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PARTY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FPCAFLAG = table.Column<bool>(type: "bit", nullable: false),
                    FBDELIVERY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FAX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MAIL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FPARTY = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voters", x => x.VUID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Voters");
        }
    }
}
