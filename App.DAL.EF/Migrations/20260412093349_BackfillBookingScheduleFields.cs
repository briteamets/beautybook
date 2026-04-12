using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBookingScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Bookings" AS b
                SET
                    "BookingDate" = b."AppointmentDateTime"::date,
                    "StartTime" = b."AppointmentDateTime"::time,
                    "EndTime" = (b."AppointmentDateTime" + make_interval(mins => COALESCE(s."DurationMinutes", 60)))::time
                FROM "Services" AS s
                WHERE b."ServiceId" = s."Id"
                  AND (b."BookingDate" = DATE '-infinity' OR b."StartTime" = INTERVAL '00:00:00');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
