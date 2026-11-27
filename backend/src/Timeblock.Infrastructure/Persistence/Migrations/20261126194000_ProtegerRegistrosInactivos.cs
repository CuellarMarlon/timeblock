using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Timeblock.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TimeblockDbContext))]
[Migration("20261126194000_ProtegerRegistrosInactivos")]
public sealed class ProtegerRegistrosInactivos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE FUNCTION timeblock_exigir_activo(tabla text, columna text, clave uuid) RETURNS void LANGUAGE plpgsql AS $$
        DECLARE valor boolean;
        BEGIN
            EXECUTE format('SELECT activo FROM %I WHERE %I = $1 FOR SHARE', tabla, columna) INTO valor USING clave;
            IF valor = false THEN
                RAISE EXCEPTION 'REGISTRO_INACTIVO: no se admite una nueva relación con un registro inactivo.' USING ERRCODE = '23514';
            END IF;
        END $$;
        CREATE FUNCTION timeblock_relacion_activa() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE programa uuid;
        BEGIN
            IF TG_TABLE_NAME = 'ESTUDIANTE' THEN
                PERFORM timeblock_exigir_activo('PERSONA', 'idPersona', NEW."idCoordinadorAsig");
            ELSIF TG_TABLE_NAME = 'MATRICULA' THEN
                PERFORM timeblock_exigir_activo('PERSONA', 'idPersona', NEW."idEstudiante");
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', NEW."idPrograma");
            ELSIF TG_TABLE_NAME = 'COORDINADOR_EN_PROGRAMA' THEN
                PERFORM timeblock_exigir_activo('PERSONA', 'idPersona', NEW."idCoordinador");
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', NEW."idPrograma");
            ELSIF TG_TABLE_NAME = 'PROGRAMA_ASIGNATURA' THEN
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', NEW."idPrograma");
                PERFORM timeblock_exigir_activo('ASIGNATURA', 'idAsignatura', NEW."idAsignatura");
            ELSIF TG_TABLE_NAME = 'BLOQUE_CONTENIDO' THEN
                PERFORM timeblock_exigir_activo('ASIGNATURA', 'idAsignatura', NEW."idAsignatura");
                PERFORM timeblock_exigir_activo('PERSONA', 'idPersona', NEW."idCoordinadorCreador");
            ELSIF TG_TABLE_NAME = 'BLOQUE_PROGRAMA' THEN
                PERFORM timeblock_exigir_activo('BLOQUE_CONTENIDO', 'idBloque', NEW."idBloque");
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', NEW."idPrograma");
            ELSIF TG_TABLE_NAME = 'HORARIO' THEN
                PERFORM timeblock_exigir_activo('PERSONA', 'idPersona', NEW."idCoordinadorGen");
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', NEW."idPrograma");
            ELSIF TG_TABLE_NAME = 'BLOQUE_EN_HORARIO' THEN
                PERFORM timeblock_exigir_activo('BLOQUE_CONTENIDO', 'idBloque', NEW."idBloque");
                SELECT "idPrograma" INTO programa FROM "HORARIO" WHERE "idHorario" = NEW."idHorario";
                PERFORM timeblock_exigir_activo('PROGRAMA_ACADEMICO', 'idPrograma', programa);
            END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_Activo_Estudiante" BEFORE INSERT OR UPDATE OF "idCoordinadorAsig" ON "ESTUDIANTE" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_Matricula" BEFORE INSERT OR UPDATE OF "idEstudiante", "idPrograma" ON "MATRICULA" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_Autorizacion" BEFORE INSERT OR UPDATE OF "idCoordinador", "idPrograma" ON "COORDINADOR_EN_PROGRAMA" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_ProgramaAsignatura" BEFORE INSERT OR UPDATE OF "idPrograma", "idAsignatura" ON "PROGRAMA_ASIGNATURA" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_Bloque" BEFORE INSERT OR UPDATE OF "idAsignatura", "idCoordinadorCreador" ON "BLOQUE_CONTENIDO" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_BloquePrograma" BEFORE INSERT OR UPDATE OF "idBloque", "idPrograma" ON "BLOQUE_PROGRAMA" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_Horario" BEFORE INSERT OR UPDATE OF "idPrograma", "idCoordinadorGen" ON "HORARIO" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        CREATE TRIGGER "TR_Activo_BloqueHorario" BEFORE INSERT OR UPDATE OF "idHorario", "idBloque" ON "BLOQUE_EN_HORARIO" FOR EACH ROW EXECUTE FUNCTION timeblock_relacion_activa();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER "TR_Activo_BloqueHorario" ON "BLOQUE_EN_HORARIO";
        DROP TRIGGER "TR_Activo_Horario" ON "HORARIO";
        DROP TRIGGER "TR_Activo_BloquePrograma" ON "BLOQUE_PROGRAMA";
        DROP TRIGGER "TR_Activo_Bloque" ON "BLOQUE_CONTENIDO";
        DROP TRIGGER "TR_Activo_ProgramaAsignatura" ON "PROGRAMA_ASIGNATURA";
        DROP TRIGGER "TR_Activo_Autorizacion" ON "COORDINADOR_EN_PROGRAMA";
        DROP TRIGGER "TR_Activo_Matricula" ON "MATRICULA";
        DROP TRIGGER "TR_Activo_Estudiante" ON "ESTUDIANTE";
        DROP FUNCTION timeblock_relacion_activa();
        DROP FUNCTION timeblock_exigir_activo(text, text, uuid);
        """);
}
