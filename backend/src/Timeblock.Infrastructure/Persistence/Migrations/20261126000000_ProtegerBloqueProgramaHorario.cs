using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Timeblock.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TimeblockDbContext))]
[Migration("20261126000000_ProtegerBloqueProgramaHorario")]
public sealed class ProtegerBloqueProgramaHorario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Columna técnica rellenada por trigger: las dos claves foráneas obligan
        // a que horario y asociación usen el mismo programa, incluso en carreras.
        // Si existen datos incompatibles, las restricciones rechazan la migración.
        migrationBuilder.Sql("""
            ALTER TABLE "HORARIO" ADD CONSTRAINT "AK_HORARIO_IdPrograma" UNIQUE ("idHorario", "idPrograma");
            ALTER TABLE "BLOQUE_EN_HORARIO" ADD COLUMN "idProgramaProteccion" uuid;
            UPDATE "BLOQUE_EN_HORARIO" AS bh
            SET "idProgramaProteccion" = h."idPrograma"
            FROM "HORARIO" AS h WHERE h."idHorario" = bh."idHorario";
            ALTER TABLE "BLOQUE_EN_HORARIO" ALTER COLUMN "idProgramaProteccion" SET NOT NULL;
            ALTER TABLE "BLOQUE_EN_HORARIO" ADD CONSTRAINT "FK_BH_HorarioPrograma"
                FOREIGN KEY ("idHorario", "idProgramaProteccion") REFERENCES "HORARIO" ("idHorario", "idPrograma") ON DELETE RESTRICT;
            ALTER TABLE "BLOQUE_EN_HORARIO" ADD CONSTRAINT "FK_BH_BloquePrograma"
                FOREIGN KEY ("idBloque", "idProgramaProteccion") REFERENCES "BLOQUE_PROGRAMA" ("idBloque", "idPrograma") ON DELETE RESTRICT;
            CREATE FUNCTION timeblock_programa_bloque_horario() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                SELECT "idPrograma" INTO NEW."idProgramaProteccion" FROM "HORARIO" WHERE "idHorario" = NEW."idHorario";
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER "TR_BH_Programa" BEFORE INSERT OR UPDATE OF "idHorario", "idProgramaProteccion"
                ON "BLOQUE_EN_HORARIO" FOR EACH ROW EXECUTE FUNCTION timeblock_programa_bloque_horario();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER "TR_BH_Programa" ON "BLOQUE_EN_HORARIO";
            DROP FUNCTION timeblock_programa_bloque_horario();
            ALTER TABLE "BLOQUE_EN_HORARIO" DROP CONSTRAINT "FK_BH_BloquePrograma";
            ALTER TABLE "BLOQUE_EN_HORARIO" DROP CONSTRAINT "FK_BH_HorarioPrograma";
            ALTER TABLE "BLOQUE_EN_HORARIO" DROP COLUMN "idProgramaProteccion";
            ALTER TABLE "HORARIO" DROP CONSTRAINT "AK_HORARIO_IdPrograma";
            """);
    }
}
