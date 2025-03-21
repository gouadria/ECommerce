using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Migrations
{
    /// <inheritdoc />
    
        /// <inheritdoc />
        public partial class RecreateIdentityColumn : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // Supprimer la colonne CartProductId existante (si nécessaire)
                migrationBuilder.DropColumn(
                    name: "CartProductId",
                    table: "CartProducts");

                // Ajouter la colonne avec la propriété IDENTITI
                migrationBuilder.AddColumn<int>(
                    name: "CartProductId",
                    table: "CartProducts",
                    nullable: false,
                    defaultValue: 0)
                    .Annotation("SqlServer:Identity", "1, 1");  // Définit la colonne comme IDENTITY
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // Inverser l'opération dans Down si nécessaire
                migrationBuilder.DropColumn(
                    name: "CartProductId",
                    table: "CartProducts");

                migrationBuilder.AddColumn<int>(
                    name: "CartProductId",
                    table: "CartProducts",
                    nullable: false,
                    defaultValue: 0);
            }
        }

    }

