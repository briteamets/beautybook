using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260414120000_SalonOwnerManagement")]
    public partial class SalonOwnerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "SalonOwners" (
                    "Id" uuid NOT NULL,
                    "SalonId" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "From" timestamp with time zone NOT NULL,
                    "To" timestamp with time zone NULL,
                    CONSTRAINT "PK_SalonOwners" PRIMARY KEY ("Id")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SalonOwners_SalonId_UserId"
                    ON "SalonOwners" ("SalonId", "UserId");

                CREATE INDEX IF NOT EXISTS "IX_SalonOwners_UserId"
                    ON "SalonOwners" ("UserId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_SalonOwners_Salons_SalonId'
                    ) THEN
                        ALTER TABLE "SalonOwners"
                            ADD CONSTRAINT "FK_SalonOwners_Salons_SalonId"
                            FOREIGN KEY ("SalonId")
                            REFERENCES "Salons" ("Id")
                            ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_SalonOwners_AspNetUsers_UserId'
                    ) THEN
                        ALTER TABLE "SalonOwners"
                            ADD CONSTRAINT "FK_SalonOwners_AspNetUsers_UserId"
                            FOREIGN KEY ("UserId")
                            REFERENCES "AspNetUsers" ("Id")
                            ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SalonOwners"
                    DROP CONSTRAINT IF EXISTS "FK_SalonOwners_AspNetUsers_UserId";

                ALTER TABLE "SalonOwners"
                    DROP CONSTRAINT IF EXISTS "FK_SalonOwners_Salons_SalonId";

                DROP INDEX IF EXISTS "IX_SalonOwners_UserId";
                DROP INDEX IF EXISTS "IX_SalonOwners_SalonId_UserId";
                DROP TABLE IF EXISTS "SalonOwners";
                """);
        }
    }
}
