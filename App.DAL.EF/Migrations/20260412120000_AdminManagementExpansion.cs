using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260412120000_AdminManagementExpansion")]
    public partial class AdminManagementExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers"
                    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();

                ALTER TABLE "AspNetUsers"
                    ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;

                ALTER TABLE "ServiceCategories"
                    ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;

                CREATE TABLE IF NOT EXISTS "ServiceTypes" (
                    "Id" uuid NOT NULL,
                    "ServiceCategoryId" uuid NOT NULL,
                    "Name" jsonb NOT NULL,
                    "Description" jsonb NULL,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    CONSTRAINT "PK_ServiceTypes" PRIMARY KEY ("Id")
                );

                ALTER TABLE "Services"
                    ADD COLUMN IF NOT EXISTS "ServiceTypeId" uuid NULL;

                ALTER TABLE "Bookings"
                    ADD COLUMN IF NOT EXISTS "CancelledByAdminId" uuid NULL;

                ALTER TABLE "Bookings"
                    ADD COLUMN IF NOT EXISTS "CancellationReason" character varying(2000) NULL;

                CREATE INDEX IF NOT EXISTS "IX_ServiceTypes_ServiceCategoryId"
                    ON "ServiceTypes" ("ServiceCategoryId");

                CREATE INDEX IF NOT EXISTS "IX_Services_ServiceTypeId"
                    ON "Services" ("ServiceTypeId");

                CREATE INDEX IF NOT EXISTS "IX_Bookings_CancelledByAdminId"
                    ON "Bookings" ("CancelledByAdminId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ServiceTypes_ServiceCategories_ServiceCategoryId'
                    ) THEN
                        ALTER TABLE "ServiceTypes"
                            ADD CONSTRAINT "FK_ServiceTypes_ServiceCategories_ServiceCategoryId"
                            FOREIGN KEY ("ServiceCategoryId")
                            REFERENCES "ServiceCategories" ("Id")
                            ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Services_ServiceTypes_ServiceTypeId'
                    ) THEN
                        ALTER TABLE "Services"
                            ADD CONSTRAINT "FK_Services_ServiceTypes_ServiceTypeId"
                            FOREIGN KEY ("ServiceTypeId")
                            REFERENCES "ServiceTypes" ("Id")
                            ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Bookings_AspNetUsers_CancelledByAdminId'
                    ) THEN
                        ALTER TABLE "Bookings"
                            ADD CONSTRAINT "FK_Bookings_AspNetUsers_CancelledByAdminId"
                            FOREIGN KEY ("CancelledByAdminId")
                            REFERENCES "AspNetUsers" ("Id")
                            ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Bookings"
                    DROP CONSTRAINT IF EXISTS "FK_Bookings_AspNetUsers_CancelledByAdminId";

                ALTER TABLE "Services"
                    DROP CONSTRAINT IF EXISTS "FK_Services_ServiceTypes_ServiceTypeId";

                ALTER TABLE "ServiceTypes"
                    DROP CONSTRAINT IF EXISTS "FK_ServiceTypes_ServiceCategories_ServiceCategoryId";

                DROP INDEX IF EXISTS "IX_Bookings_CancelledByAdminId";
                DROP INDEX IF EXISTS "IX_Services_ServiceTypeId";
                DROP INDEX IF EXISTS "IX_ServiceTypes_ServiceCategoryId";

                DROP TABLE IF EXISTS "ServiceTypes";

                ALTER TABLE "ServiceCategories"
                    DROP COLUMN IF EXISTS "IsActive";

                ALTER TABLE "Services"
                    DROP COLUMN IF EXISTS "ServiceTypeId";

                ALTER TABLE "Bookings"
                    DROP COLUMN IF EXISTS "CancelledByAdminId";

                ALTER TABLE "Bookings"
                    DROP COLUMN IF EXISTS "CancellationReason";

                ALTER TABLE "AspNetUsers"
                    DROP COLUMN IF EXISTS "CreatedAt";

                ALTER TABLE "AspNetUsers"
                    DROP COLUMN IF EXISTS "IsActive";
                """);
        }
    }
}
