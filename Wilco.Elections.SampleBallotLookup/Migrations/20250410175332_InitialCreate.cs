using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilco.Elections.SampleBallotLookup.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BallotStyleLinks",
                columns: table => new
                {
                    StyleCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrecinctID = table.Column<int>(type: "int", nullable: false),
                    SplitID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QALink = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotStyleLinks", x => x.StyleCode);
                });

            migrationBuilder.CreateTable(
                name: "BallotStyles",
                columns: table => new
                {
                    VUID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PCT_CODE = table.Column<int>(type: "int", nullable: false),
                    LABEL = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotStyles", x => x.VUID);
                });
            migrationBuilder.CreateTable(
                name: "Voters",
                columns: table => new
                {
                    VUID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PCT_CODE = table.Column<int>(type: "int", nullable: false),
                    LABEL = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "BallotStyleLinks");

            migrationBuilder.DropTable(
                name: "BallotStyles");
        }
    }
}
