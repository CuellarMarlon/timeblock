using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timeblock.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// RII04 y clases sin solapar dentro de un horario. El índice único de M4
    /// sobre aula, fecha y hora de inicio solo impide dos imparticiones que
    /// empiecen a la vez; estas restricciones de exclusión rechazan cualquier
    /// intervalo <c>[inicio, fin)</c> que se corte con otro de la misma aula y
    /// fecha, o del mismo horario aunque use otra aula. Los intervalos
    /// contiguos no se cortan. PostgreSQL comprueba la exclusión también frente
    /// a filas que otra transacción aún no confirmó: la segunda espera y se
    /// rechaza si la primera confirma.
    /// </summary>
    public partial class ImparticionesSinSolapes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.Sql(
                """
                ALTER TABLE "BLOQUE_EN_HORARIO"
                    ADD CONSTRAINT "EX_BLOQUE_EN_HORARIO_AulaSinSolapes"
                    EXCLUDE USING gist (
                        "idAula" WITH =,
                        tsrange("fecha" + "horaInicio", "fecha" + "horaFin", '[)') WITH &&
                    )
                    WHERE ("idAula" IS NOT NULL);

                ALTER TABLE "BLOQUE_EN_HORARIO"
                    ADD CONSTRAINT "EX_BLOQUE_EN_HORARIO_HorarioSinSolapes"
                    EXCLUDE USING gist (
                        "idHorario" WITH =,
                        tsrange("fecha" + "horaInicio", "fecha" + "horaFin", '[)') WITH &&
                    )
                    WHERE ("horaInicio" IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BLOQUE_EN_HORARIO" DROP CONSTRAINT IF EXISTS "EX_BLOQUE_EN_HORARIO_HorarioSinSolapes";
                ALTER TABLE "BLOQUE_EN_HORARIO" DROP CONSTRAINT IF EXISTS "EX_BLOQUE_EN_HORARIO_AulaSinSolapes";
                """);

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
        }
    }
}
