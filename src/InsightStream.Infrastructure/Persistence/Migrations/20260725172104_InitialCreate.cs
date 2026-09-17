using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    medium_handle = table.Column<string>(type: "text", nullable: true),
                    custom_domain = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    url_hash = table.Column<string>(type: "char(64)", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    first_seen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority_score = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 5.00m),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    received_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    destination_email = table.Column<string>(type: "text", nullable: false),
                    raw_content = table.Column<string>(type: "text", nullable: false),
                    email_hash = table.Column<string>(type: "char(64)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    last_login = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "link_authors",
                columns: table => new
                {
                    link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_link_authors", x => new { x.link_id, x.author_id });
                    table.ForeignKey(
                        name: "fk_link_authors_authors_author_id",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_link_authors_links_link_id",
                        column: x => x.link_id,
                        principalTable: "links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_links",
                columns: table => new
                {
                    newsletter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletter_links", x => new { x.newsletter_id, x.link_id });
                    table.ForeignKey(
                        name: "fk_newsletter_links_links_link_id",
                        column: x => x.link_id,
                        principalTable: "links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_newsletter_links_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preference_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    preference_value = table.Column<string>(type: "text", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                    table.CheckConstraint("ck_user_preferences_weight_range", "weight >= -1.00 AND weight <= 1.00");
                    table.ForeignKey(
                        name: "fk_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "last_login", "password_hash", "updated_at", "username" },
                values: new object[] { new Guid("00000000-0000-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "$2a$11$Lemo1jqJeszZ7t4DtrQe3.KDbMT67aBQa9adkEDaLH7im39QgUMuq", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin" });

            migrationBuilder.CreateIndex(
                name: "ix_authors_custom_domain",
                table: "authors",
                column: "custom_domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authors_medium_handle",
                table: "authors",
                column: "medium_handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_link_authors_author_id",
                table: "link_authors",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_links_priority_score",
                table: "links",
                column: "priority_score",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_links_status",
                table: "links",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_links_url_hash",
                table: "links",
                column: "url_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_links_link_id",
                table: "newsletter_links",
                column: "link_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_email_hash",
                table: "newsletters",
                column: "email_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_user_id_preference_type_preference_value",
                table: "user_preferences",
                columns: new[] { "user_id", "preference_type", "preference_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "link_authors");

            migrationBuilder.DropTable(
                name: "newsletter_links");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "links");

            migrationBuilder.DropTable(
                name: "newsletters");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
