using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Migrations
{
    public partial class ChangePaymentIdType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Supprimer la clé étrangère de Orders vers Payments
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payments_PayementId",
                table: "Orders");

            // 2. Supprimer l'index sur PayementId dans Orders (s'il existe)
            migrationBuilder.DropIndex(
                name: "IX_Orders_PayementId",
                table: "Orders");

            // 3. Supprimer la clé primaire existante sur Payments
            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            // 4. Supprimer la colonne PaymentId (int)
            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Payments");

            // 5. Ajouter une nouvelle colonne PaymentId de type string
            migrationBuilder.AddColumn<string>(
                name: "PaymentId",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: false);

            // 6. Définir PaymentId comme nouvelle clé primaire
            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "PaymentId");

            // 7. Modifier la colonne PayementId dans Orders pour correspondre au nouveau type
            migrationBuilder.AlterColumn<string>(
                name: "PayementId",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false);

            // 8. Recréer l'index sur PayementId dans Orders
            migrationBuilder.CreateIndex(
                name: "IX_Orders_PayementId",
                table: "Orders",
                column: "PayementId");

            // 9. Recréer la clé étrangère avec le nouveau type
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payments_PayementId",
                table: "Orders",
                column: "PayementId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Supprimer la clé étrangère
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payments_PayementId",
                table: "Orders");

            // 2. Supprimer l'index sur PayementId
            migrationBuilder.DropIndex(
                name: "IX_Orders_PayementId",
                table: "Orders");

            // 3. Supprimer la clé primaire existante
            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            // 4. Supprimer la nouvelle colonne PaymentId (string)
            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Payments");

            // 5. Ajouter à nouveau la colonne PaymentId en int
            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Payments",
                type: "int",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            // 6. Définir PaymentId comme clé primaire
            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "PaymentId");

            // 7. Modifier la colonne PayementId dans Orders pour revenir à int
            migrationBuilder.AlterColumn<int>(
                name: "PayementId",
                table: "Orders",
                type: "int",
                nullable: false);

            // 8. Recréer l'index sur PayementId
            migrationBuilder.CreateIndex(
                name: "IX_Orders_PayementId",
                table: "Orders",
                column: "PayementId");

            // 9. Recréer la clé étrangère avec int
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payments_PayementId",
                table: "Orders",
                column: "PayementId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}





