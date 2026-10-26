using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClyvoVetApi.Migrations
{
    /// <inheritdoc />
    public partial class OracleBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Esta baseline cria um schema novo. Bancos existentes exigem conferência e adoção manual.
            migrationBuilder.Sql("""
                DECLARE
                    existing_tables NUMBER;
                BEGIN
                    SELECT COUNT(*) INTO existing_tables FROM USER_TABLES
                    WHERE TABLE_NAME IN ('DONOS', 'TUTORES', 'FUNCIONARIOS', 'MEDICAMENTOS',
                        'PETS', 'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS');
                    IF existing_tables > 0 THEN
                        RAISE_APPLICATION_ERROR(-20001,
                            'Schema clinico existente. Nao aplicar baseline; consulte docs/database/README.md.');
                    END IF;
                END;
                """);

            migrationBuilder.CreateTable(
                name: "DONOS",
                columns: table => new
                {
                    dono_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    dono_nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    dono_email = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    dono_telefone = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DONOS_PK", x => x.dono_id);
                });

            migrationBuilder.CreateTable(
                name: "FUNCIONARIOS",
                columns: table => new
                {
                    funcionario_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    funcionario_nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    funcionario_setor = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    funcionario_cargo = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    funcionario_email = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    funcionario_telefone = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("FUNCIONARIOS_PK", x => x.funcionario_id);
                });

            migrationBuilder.CreateTable(
                name: "MEDICAMENTOS",
                columns: table => new
                {
                    medicamento_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    medicamento_nome = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("MEDICAMENTOS_PK", x => x.medicamento_id);
                });

            migrationBuilder.CreateTable(
                name: "PETS",
                columns: table => new
                {
                    pet_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    pet_nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    pet_especie = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    pet_raca = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    pet_data_nascimento = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    pet_peso = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    dono_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PETS_PK", x => x.pet_id);
                    table.ForeignKey(
                        name: "PETS_DONOS_FK",
                        column: x => x.dono_id,
                        principalTable: "DONOS",
                        principalColumn: "dono_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CONSULTAS",
                columns: table => new
                {
                    consulta_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    consulta_tipo = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    consulta_valor = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    consulta_descricao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    consulta_status = table.Column<string>(type: "NCHAR(1)", fixedLength: true, maxLength: 1, nullable: false),
                    consulta_data = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    pet_id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    funcionario_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CONSULTAS_PK", x => x.consulta_id);
                    table.ForeignKey(
                        name: "CONSULTAS_FUNCIONARIOS_FK",
                        column: x => x.funcionario_id,
                        principalTable: "FUNCIONARIOS",
                        principalColumn: "funcionario_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "CONSULTAS_PETS_FK",
                        column: x => x.pet_id,
                        principalTable: "PETS",
                        principalColumn: "pet_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VACINAS",
                columns: table => new
                {
                    vacina_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    vacina_nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    vacina_data = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    vacina_status = table.Column<string>(type: "NCHAR(1)", fixedLength: true, maxLength: 1, nullable: false),
                    pet_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("VACINAS_PK", x => x.vacina_id);
                    table.ForeignKey(
                        name: "VACINAS_PETS_FK",
                        column: x => x.pet_id,
                        principalTable: "PETS",
                        principalColumn: "pet_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CONSULTAS_MEDICAMENTOS",
                columns: table => new
                {
                    consulta_medicamento_id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    consulta_id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    medicamento_id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    cm_dosagem = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CONSULTAS_MEDICAMENTOS_PK", x => x.consulta_medicamento_id);
                    table.ForeignKey(
                        name: "CM_CONSULTAS_FK",
                        column: x => x.consulta_id,
                        principalTable: "CONSULTAS",
                        principalColumn: "consulta_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "CM_MEDICAMENTOS_FK",
                        column: x => x.medicamento_id,
                        principalTable: "MEDICAMENTOS",
                        principalColumn: "medicamento_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_funcionario_id",
                table: "CONSULTAS",
                column: "funcionario_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_pet_id",
                table: "CONSULTAS",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_MEDICAMENTOS_consulta_id",
                table: "CONSULTAS_MEDICAMENTOS",
                column: "consulta_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_MEDICAMENTOS_medicamento_id",
                table: "CONSULTAS_MEDICAMENTOS",
                column: "medicamento_id");

            migrationBuilder.CreateIndex(
                name: "UQ_DONO_EMAIL",
                table: "DONOS",
                column: "dono_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_FUNC_EMAIL",
                table: "FUNCIONARIOS",
                column: "funcionario_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PETS_dono_id",
                table: "PETS",
                column: "dono_id");

            migrationBuilder.CreateIndex(
                name: "IX_VACINAS_pet_id",
                table: "VACINAS",
                column: "pet_id");

            // Oracle EF 9.23.60 não escapa literais de checks no CREATE TABLE dinâmico.
            migrationBuilder.Sql("""
                ALTER TABLE "CONSULTAS" ADD CONSTRAINT "CK_CONSULTA_STATUS"
                CHECK ("consulta_status" IN ('A', 'C', 'R'))
                """);
            migrationBuilder.Sql("""
                ALTER TABLE "VACINAS" ADD CONSTRAINT "CK_VACINA_STATUS"
                CHECK ("vacina_status" IN ('P', 'A'))
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONSULTAS_MEDICAMENTOS");

            migrationBuilder.DropTable(
                name: "VACINAS");

            migrationBuilder.DropTable(
                name: "CONSULTAS");

            migrationBuilder.DropTable(
                name: "MEDICAMENTOS");

            migrationBuilder.DropTable(
                name: "FUNCIONARIOS");

            migrationBuilder.DropTable(
                name: "PETS");

            migrationBuilder.DropTable(
                name: "DONOS");
        }
    }
}
