using System;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaKioskRecommender.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260608203300_NormalizeDatabaseStructure")]
    public partial class NormalizeDatabaseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var hall1Id = new Guid("11111111-1111-1111-1111-111111111111");
            var hall2Id = new Guid("22222222-2222-2222-2222-222222222222");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SeatId",
                table: "Tickets");

            migrationBuilder.CreateTable(
                name: "Halls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Halls", x => x.Id);
                });

            migrationBuilder.Sql($"""
                INSERT INTO Halls (Id, Name, Capacity)
                VALUES
                    ('{hall1Id}', 'Зал 1', 50),
                    ('{hall2Id}', 'Зал 2', 50);
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "HallId",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: hall1Id);

            migrationBuilder.Sql($"""
                UPDATE Sessions
                SET HallId = CASE
                    WHEN HallNumber = 2 THEN '{hall2Id}'
                    ELSE '{hall1Id}'
                END;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientProfileId",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Tickets
                SET ClientProfileId = (
                    SELECT ClientProfiles.Id
                    FROM ClientProfiles
                    WHERE ClientProfiles.PhoneNumber = Tickets.PhoneNumber
                    LIMIT 1
                )
                WHERE PhoneNumber IS NOT NULL AND PhoneNumber <> '';
                """);

            migrationBuilder.Sql("""
                CREATE TEMP TABLE TicketSeatPairs (
                    TicketId TEXT NOT NULL,
                    SeatId TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO TicketSeatPairs (TicketId, SeatId)
                SELECT Id, SeatId
                FROM Tickets
                WHERE SeatId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                WITH RECURSIVE split(TicketId, SessionId, SeatNumber, Rest) AS (
                    SELECT Id, SessionId, '', SeatNumbers || ','
                    FROM Tickets
                    WHERE SeatNumbers IS NOT NULL AND SeatNumbers <> ''

                    UNION ALL

                    SELECT
                        TicketId,
                        SessionId,
                        trim(substr(Rest, 0, instr(Rest, ','))),
                        substr(Rest, instr(Rest, ',') + 1)
                    FROM split
                    WHERE Rest <> ''
                )
                INSERT OR IGNORE INTO TicketSeatPairs (TicketId, SeatId)
                SELECT split.TicketId, Seats.Id
                FROM split
                INNER JOIN Seats
                    ON Seats.SessionId = split.SessionId
                    AND Seats.SeatNumber = split.SeatNumber
                WHERE split.SeatNumber <> '';
                """);

            migrationBuilder.Sql("""
                PRAGMA foreign_keys = OFF;

                CREATE TABLE Tickets_new (
                    Id TEXT NOT NULL CONSTRAINT PK_Tickets PRIMARY KEY,
                    SessionId TEXT NOT NULL,
                    ClientProfileId TEXT NULL,
                    TotalPrice TEXT NOT NULL,
                    PurchasedAt TEXT NOT NULL,
                    KioskSessionId TEXT NULL,
                    CONSTRAINT FK_Tickets_ClientProfiles_ClientProfileId
                        FOREIGN KEY (ClientProfileId) REFERENCES ClientProfiles (Id) ON DELETE SET NULL,
                    CONSTRAINT FK_Tickets_KioskSessions_KioskSessionId
                        FOREIGN KEY (KioskSessionId) REFERENCES KioskSessions (Id),
                    CONSTRAINT FK_Tickets_Sessions_SessionId
                        FOREIGN KEY (SessionId) REFERENCES Sessions (Id) ON DELETE RESTRICT
                );

                INSERT INTO Tickets_new (Id, SessionId, ClientProfileId, TotalPrice, PurchasedAt, KioskSessionId)
                SELECT Id, SessionId, ClientProfileId, TotalPrice, PurchasedAt, KioskSessionId
                FROM Tickets;

                DROP TABLE Tickets;
                ALTER TABLE Tickets_new RENAME TO Tickets;

                PRAGMA foreign_keys = ON;
                """);

            migrationBuilder.CreateTable(
                name: "TicketSeats",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SeatId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSeats", x => new { x.TicketId, x.SeatId });
                    table.ForeignKey(
                        name: "FK_TicketSeats_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketSeats_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO TicketSeats (TicketId, SeatId)
                SELECT TicketId, SeatId
                FROM TicketSeatPairs;

                DROP TABLE TicketSeatPairs;
                """);

            migrationBuilder.Sql("ALTER TABLE Sessions DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("ALTER TABLE Sessions DROP COLUMN HallNumber;");
            migrationBuilder.Sql("ALTER TABLE Sessions DROP COLUMN UpdatedAt;");

            migrationBuilder.Sql("ALTER TABLE Seats DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("ALTER TABLE Seats DROP COLUMN UpdatedAt;");

            migrationBuilder.Sql("ALTER TABLE Movies DROP COLUMN AverageRating;");
            migrationBuilder.Sql("ALTER TABLE Movies DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("ALTER TABLE Movies DROP COLUMN Director;");
            migrationBuilder.Sql("ALTER TABLE Movies DROP COLUMN UpdatedAt;");

            migrationBuilder.Sql("ALTER TABLE KioskSessions DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("ALTER TABLE KioskSessions DROP COLUMN UpdatedAt;");

            migrationBuilder.Sql("ALTER TABLE ClientProfiles DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("ALTER TABLE ClientProfiles DROP COLUMN UpdatedAt;");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ClientProfileId",
                table: "Tickets",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HallId",
                table: "Sessions",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSeats_SeatId",
                table: "TicketSeats",
                column: "SeatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_ClientProfileId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_HallId",
                table: "Sessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Tickets",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeatId",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatNumbers",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "HallNumber",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Seats",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Seats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Movies",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Movies",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Movies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Movies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "KioskSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "KioskSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ClientProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ClientProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Tickets
                SET SeatId = (
                    SELECT TicketSeats.SeatId
                    FROM TicketSeats
                    WHERE TicketSeats.TicketId = Tickets.Id
                    LIMIT 1
                );
                """);

            migrationBuilder.Sql("""
                UPDATE Tickets
                SET SeatNumbers = (
                    SELECT group_concat(Seats.SeatNumber, ',')
                    FROM TicketSeats
                    INNER JOIN Seats ON Seats.Id = TicketSeats.SeatId
                    WHERE TicketSeats.TicketId = Tickets.Id
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM TicketSeats
                    WHERE TicketSeats.TicketId = Tickets.Id
                );
                """);

            migrationBuilder.Sql("""
                UPDATE Tickets
                SET PhoneNumber = (
                    SELECT ClientProfiles.PhoneNumber
                    FROM ClientProfiles
                    WHERE ClientProfiles.Id = Tickets.ClientProfileId
                    LIMIT 1
                )
                WHERE ClientProfileId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE Sessions
                SET HallNumber = CASE
                    WHEN (SELECT Name FROM Halls WHERE Halls.Id = Sessions.HallId) = 'Зал 2' THEN 2
                    ELSE 1
                END;
                """);

            migrationBuilder.DropTable(
                name: "TicketSeats");

            migrationBuilder.Sql("ALTER TABLE Tickets DROP COLUMN ClientProfileId;");

            migrationBuilder.Sql("ALTER TABLE Sessions DROP COLUMN HallId;");

            migrationBuilder.DropTable(
                name: "Halls");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SeatId",
                table: "Tickets",
                column: "SeatId");
        }
    }
}
