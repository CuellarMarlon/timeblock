using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;

namespace Timeblock.Domain.Tests.Bloques;

public class BloqueContenidoTests
{
    private static readonly Guid AsignaturaId = Guid.NewGuid();
    private static readonly Guid CoordinadorId = Guid.NewGuid();

    private static BloqueContenido CrearBloque(
        string descripcion = "Introducción a las bases de datos",
        TipoActividad tipo = TipoActividad.Teoria,
        NivelComplejidad nivel = NivelComplejidad.Medio,
        Guid? asignaturaId = null,
        Guid? coordinadorId = null) =>
        BloqueContenido.Crear(
            descripcion,
            tipo,
            nivel,
            asignaturaId ?? AsignaturaId,
            coordinadorId ?? CoordinadorId);

    private static ProgramaAcademico CrearPrograma(params Guid[] asignaturas)
    {
        var programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Ingeniería Informática", "Plan 2024");

        foreach (Guid asignatura in asignaturas)
        {
            programa.AsociarAsignatura(asignatura);
        }

        return programa;
    }

    // --- Creación y valores permitidos ---

    [Fact]
    public void Crear_Valido_ConservaDatos()
    {
        var id = Guid.NewGuid();

        var bloque = BloqueContenido.Crear(
            id,
            "  Introducción   a las BASES de datos ",
            TipoActividad.Laboratorio,
            NivelComplejidad.Alto,
            AsignaturaId,
            CoordinadorId);

        Assert.Equal(id, bloque.Id);
        Assert.Equal("Introducción a las BASES de datos", bloque.Descripcion);
        Assert.Equal("introducción a las bases de datos", bloque.DescripcionNormalizada);
        Assert.Equal(TipoActividad.Laboratorio, bloque.TipoActividad);
        Assert.Equal(NivelComplejidad.Alto, bloque.NivelComplejidad);
        Assert.Equal(AsignaturaId, bloque.AsignaturaId);
        Assert.Equal(CoordinadorId, bloque.CoordinadorCreadorId);
        Assert.Empty(bloque.Programas);
    }

    [Fact]
    public void Crear_SinId_GeneraIdNuevo()
    {
        var primero = CrearBloque();
        var segundo = CrearBloque();

        Assert.NotEqual(Guid.Empty, primero.Id);
        Assert.NotEqual(primero.Id, segundo.Id);
    }

    [Fact]
    public void TipoActividad_SoloAdmiteTeoriaPracticaOLaboratorio()
    {
        Assert.Equal(
            ["Teoria", "Practica", "Laboratorio"],
            Enum.GetNames<TipoActividad>());
    }

    [Fact]
    public void NivelComplejidad_SoloAdmiteBajoMedioOAlto()
    {
        Assert.Equal(
            ["Bajo", "Medio", "Alto"],
            Enum.GetNames<NivelComplejidad>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    public void Crear_TipoActividadInvalido_Falla(int valor)
    {
        Assert.Throws<DomainException>(() => CrearBloque(tipo: (TipoActividad)valor));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    public void Crear_NivelComplejidadInvalido_Falla(int valor)
    {
        Assert.Throws<DomainException>(() => CrearBloque(nivel: (NivelComplejidad)valor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_DescripcionVacia_Falla(string descripcion)
    {
        Assert.Throws<DomainException>(() => CrearBloque(descripcion));
    }

    [Fact]
    public void Crear_IdVacio_Falla()
    {
        Assert.Throws<DomainException>(
            () => BloqueContenido.Crear(
                Guid.Empty,
                "Intro",
                TipoActividad.Teoria,
                NivelComplejidad.Bajo,
                AsignaturaId,
                CoordinadorId));
    }

    [Fact]
    public void Crear_AsignaturaVacia_Falla()
    {
        Assert.Throws<DomainException>(() => CrearBloque(asignaturaId: Guid.Empty));
    }

    [Fact]
    public void Crear_CoordinadorVacio_Falla()
    {
        Assert.Throws<DomainException>(() => CrearBloque(coordinadorId: Guid.Empty));
    }

    // --- Duplicidad técnica ---

    [Fact]
    public void EsDuplicadoDe_MismaAsignaturaTipoYDescripcionNormalizada_EsTrue()
    {
        var existente = CrearBloque("Introducción a SQL");
        var nuevo = CrearBloque("  INTRODUCCIÓN   a   sql ");

        Assert.True(nuevo.EsDuplicadoDe(existente));
        Assert.True(existente.EsDuplicadoDe(nuevo));
        Assert.Equal(existente.ClaveDuplicidad, nuevo.ClaveDuplicidad);
    }

    [Fact]
    public void EsDuplicadoDe_TipoDistinto_EsFalse()
    {
        var teoria = CrearBloque("Intro", TipoActividad.Teoria);
        var practica = CrearBloque("Intro", TipoActividad.Practica);

        Assert.False(teoria.EsDuplicadoDe(practica));
    }

    [Fact]
    public void EsDuplicadoDe_AsignaturaDistinta_EsFalse()
    {
        var uno = CrearBloque("Intro");
        var otro = CrearBloque("Intro", asignaturaId: Guid.NewGuid());

        Assert.False(uno.EsDuplicadoDe(otro));
    }

    [Fact]
    public void EsDuplicadoDe_DescripcionDistinta_EsFalse()
    {
        Assert.False(CrearBloque("Intro").EsDuplicadoDe(CrearBloque("Introducción")));
    }

    [Fact]
    public void EsDuplicadoDe_AcentosDistintos_EsFalse()
    {
        Assert.False(CrearBloque("Camión").EsDuplicadoDe(CrearBloque("Camion")));
    }

    [Fact]
    public void EsDuplicadoDe_ComplejidadOCoordinadorDistintos_NoInfluyen()
    {
        var uno = CrearBloque("Intro", nivel: NivelComplejidad.Bajo, coordinadorId: Guid.NewGuid());
        var otro = CrearBloque("Intro", nivel: NivelComplejidad.Alto, coordinadorId: Guid.NewGuid());

        Assert.True(uno.EsDuplicadoDe(otro));
    }

    [Fact]
    public void EsDuplicadoDe_ElMismoBloque_EsFalse()
    {
        var bloque = CrearBloque();

        Assert.False(bloque.EsDuplicadoDe(bloque));
    }

    // --- Actualización de datos ---

    [Fact]
    public void ActualizarDatos_Validos_CambiaDatosYClave()
    {
        var bloque = CrearBloque("Intro", TipoActividad.Teoria, NivelComplejidad.Bajo);
        var claveAnterior = bloque.ClaveDuplicidad;

        bloque.ActualizarDatos("  Práctica   de SQL ", TipoActividad.Practica, NivelComplejidad.Alto);

        Assert.Equal("Práctica de SQL", bloque.Descripcion);
        Assert.Equal("práctica de sql", bloque.DescripcionNormalizada);
        Assert.Equal(TipoActividad.Practica, bloque.TipoActividad);
        Assert.Equal(NivelComplejidad.Alto, bloque.NivelComplejidad);
        Assert.NotEqual(claveAnterior, bloque.ClaveDuplicidad);
    }

    [Fact]
    public void ActualizarDatos_DescripcionVacia_NoModificaElBloque()
    {
        var bloque = CrearBloque("Intro", TipoActividad.Teoria, NivelComplejidad.Bajo);

        Assert.Throws<DomainException>(
            () => bloque.ActualizarDatos(" ", TipoActividad.Practica, NivelComplejidad.Alto));

        Assert.Equal("Intro", bloque.Descripcion);
        Assert.Equal(TipoActividad.Teoria, bloque.TipoActividad);
        Assert.Equal(NivelComplejidad.Bajo, bloque.NivelComplejidad);
    }

    [Fact]
    public void ActualizarDatos_TipoInvalido_NoModificaElBloque()
    {
        var bloque = CrearBloque("Intro");

        Assert.Throws<DomainException>(
            () => bloque.ActualizarDatos("Otra", (TipoActividad)99, NivelComplejidad.Alto));

        Assert.Equal("Intro", bloque.Descripcion);
        Assert.Equal(NivelComplejidad.Medio, bloque.NivelComplejidad);
    }

    [Fact]
    public void ActualizarDatos_NivelInvalido_NoModificaElBloque()
    {
        var bloque = CrearBloque("Intro");

        Assert.Throws<DomainException>(
            () => bloque.ActualizarDatos("Otra", TipoActividad.Practica, (NivelComplejidad)99));

        Assert.Equal("Intro", bloque.Descripcion);
        Assert.Equal(TipoActividad.Teoria, bloque.TipoActividad);
    }

    // --- Asociación con programas (RII03) ---

    [Fact]
    public void AsociarPrograma_ProgramaQueIncluyeLaAsignatura_AgregaAsociacion()
    {
        var bloque = CrearBloque();
        var programa = CrearPrograma(AsignaturaId);

        bloque.AsociarPrograma(programa);

        BloquePrograma asociacion = Assert.Single(bloque.Programas);
        Assert.Equal(bloque.Id, asociacion.BloqueId);
        Assert.Equal(programa.Id, asociacion.ProgramaId);
        Assert.True(bloque.EstaAsociadoAPrograma(programa.Id));
    }

    [Fact]
    public void AsociarPrograma_ProgramaSinLaAsignatura_FallaYNoAsocia()
    {
        var bloque = CrearBloque();
        var programa = CrearPrograma(Guid.NewGuid());

        Assert.Throws<DomainException>(() => bloque.AsociarPrograma(programa));

        Assert.Empty(bloque.Programas);
    }

    [Fact]
    public void AsociarPrograma_ProgramaSinAsignaturas_Falla()
    {
        var bloque = CrearBloque();

        Assert.Throws<DomainException>(() => bloque.AsociarPrograma(CrearPrograma()));
    }

    [Fact]
    public void AsociarPrograma_DosVeces_EsIdempotente()
    {
        var bloque = CrearBloque();
        var programa = CrearPrograma(AsignaturaId);

        bloque.AsociarPrograma(programa);
        bloque.AsociarPrograma(programa);

        Assert.Single(bloque.Programas);
    }

    [Fact]
    public void AsociarPrograma_VariosProgramas_ConservaTodos()
    {
        var bloque = CrearBloque();
        var primero = CrearPrograma(AsignaturaId);
        var segundo = CrearPrograma(AsignaturaId, Guid.NewGuid());

        bloque.AsociarPrograma(primero);
        bloque.AsociarPrograma(segundo);

        Assert.Equal(2, bloque.Programas.Count);
    }

    [Fact]
    public void AsociarPrograma_Nulo_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CrearBloque().AsociarPrograma(null!));
    }

    // --- Cambio de asignatura ---

    [Fact]
    public void CambiarAsignatura_TodosLosProgramasIncluyenLaNueva_CambiaYConservaAsociaciones()
    {
        var nuevaAsignaturaId = Guid.NewGuid();
        var bloque = CrearBloque();
        var primero = CrearPrograma(AsignaturaId, nuevaAsignaturaId);
        var segundo = CrearPrograma(AsignaturaId, nuevaAsignaturaId);
        bloque.AsociarPrograma(primero);
        bloque.AsociarPrograma(segundo);

        bloque.CambiarAsignatura(nuevaAsignaturaId, [primero, segundo]);

        Assert.Equal(nuevaAsignaturaId, bloque.AsignaturaId);
        Assert.Equal(2, bloque.Programas.Count);
        Assert.Equal(nuevaAsignaturaId, bloque.ClaveDuplicidad.AsignaturaId);
    }

    [Fact]
    public void CambiarAsignatura_UnProgramaNoIncluyeLaNueva_FallaYNoCambia()
    {
        var nuevaAsignaturaId = Guid.NewGuid();
        var bloque = CrearBloque();
        var valido = CrearPrograma(AsignaturaId, nuevaAsignaturaId);
        var invalido = CrearPrograma(AsignaturaId);
        bloque.AsociarPrograma(valido);
        bloque.AsociarPrograma(invalido);

        Assert.Throws<DomainException>(
            () => bloque.CambiarAsignatura(nuevaAsignaturaId, [valido, invalido]));

        Assert.Equal(AsignaturaId, bloque.AsignaturaId);
        Assert.Equal(2, bloque.Programas.Count);
    }

    [Fact]
    public void CambiarAsignatura_SinProgramasAsociados_Cambia()
    {
        var nuevaAsignaturaId = Guid.NewGuid();
        var bloque = CrearBloque();

        bloque.CambiarAsignatura(nuevaAsignaturaId, []);

        Assert.Equal(nuevaAsignaturaId, bloque.AsignaturaId);
    }

    [Fact]
    public void CambiarAsignatura_FaltaUnProgramaAsociado_FallaYNoCambia()
    {
        var nuevaAsignaturaId = Guid.NewGuid();
        var bloque = CrearBloque();
        var asociado = CrearPrograma(AsignaturaId, nuevaAsignaturaId);
        bloque.AsociarPrograma(asociado);

        Assert.Throws<ArgumentException>(() => bloque.CambiarAsignatura(nuevaAsignaturaId, []));

        Assert.Equal(AsignaturaId, bloque.AsignaturaId);
    }

    [Fact]
    public void CambiarAsignatura_ProgramasNoAsociadosEnLaLista_SeIgnoran()
    {
        var nuevaAsignaturaId = Guid.NewGuid();
        var bloque = CrearBloque();
        var asociado = CrearPrograma(AsignaturaId, nuevaAsignaturaId);
        var ajeno = CrearPrograma(AsignaturaId);
        bloque.AsociarPrograma(asociado);

        bloque.CambiarAsignatura(nuevaAsignaturaId, [asociado, ajeno]);

        Assert.Equal(nuevaAsignaturaId, bloque.AsignaturaId);
    }

    [Fact]
    public void CambiarAsignatura_IdVacio_Falla()
    {
        Assert.Throws<DomainException>(() => CrearBloque().CambiarAsignatura(Guid.Empty, []));
    }

    [Fact]
    public void CambiarAsignatura_ListaNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => CrearBloque().CambiarAsignatura(Guid.NewGuid(), null!));
    }

    // --- Uso por otros coordinadores (RF15) ---

    [Fact]
    public void PuedeUsarseEn_BloqueDeOtroCoordinadorAsociadoAlPrograma_EsTrue()
    {
        var bloque = CrearBloque(coordinadorId: Guid.NewGuid());
        var programa = CrearPrograma(AsignaturaId);
        bloque.AsociarPrograma(programa);

        Assert.True(bloque.PuedeUsarseEn(programa.Id));
    }

    [Fact]
    public void PuedeUsarseEn_ProgramaSinAsociar_EsFalse()
    {
        var bloque = CrearBloque();
        var programa = CrearPrograma(AsignaturaId);

        Assert.False(bloque.PuedeUsarseEn(programa.Id));
    }
}
