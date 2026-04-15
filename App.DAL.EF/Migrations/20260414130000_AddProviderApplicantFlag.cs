using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260414130000_AddProviderApplicantFlag")]
    public partial class AddProviderApplicantFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers"
                    ADD COLUMN IF NOT EXISTS "IsProviderApplicant" boolean NOT NULL DEFAULT FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers"
                    DROP COLUMN IF EXISTS "IsProviderApplicant";
                """);
        }
    }
}
