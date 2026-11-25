using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Cria as entidades pai exigidas pelas FKs de domínio quando uma fixture usa
/// identificadores fictícios. Não altera asserts nem regras de negócio.
/// </summary>
internal static class SemeaduraIntegridade
{
    /// <summary>Cria os pais Faltantes para todas as entidades rastreadas como Added.</summary>
    public static void GarantirTudo(ContextoPontoBarber contexto)
    {
        foreach (var perfil in contexto.ChangeTracker.Entries<PerfilProfissional>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
            GarantirUsuario(contexto, perfil.UsuarioId);

        foreach (var barbearia in contexto.ChangeTracker.Entries<Barbearia>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
            GarantirProfissional(contexto, barbearia.IdProfissionalProprietario);

        foreach (var vinculo in contexto.ChangeTracker.Entries<VinculoProfissional>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
        {
            GarantirBarbearia(contexto, vinculo.BarbeariaId);
            GarantirPerfil(contexto, vinculo.ProfissionalId, $"prof-{vinculo.ProfissionalId:N}");
        }

        foreach (var agendamento in contexto.ChangeTracker.Entries<Agendamento>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
        {
            GarantirBarbearia(contexto, agendamento.BarbeariaId);
            GarantirServico(contexto, agendamento.BarbeariaId, agendamento.ServicoId);
            GarantirVinculo(contexto, agendamento.BarbeariaId, agendamento.ProfissionalId);
        }

        foreach (var pagamento in contexto.ChangeTracker.Entries<Pagamento>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
            GarantirAgendamento(
                contexto, pagamento.BarbeariaId, pagamento.AgendamentoId,
                Guid.NewGuid(), pagamento.RegistradoPorUsuarioId);

        foreach (var notificacao in contexto.ChangeTracker.Entries<Notificacao>()
                     .Where(item => item.State == EntityState.Added)
                     .Select(item => item.Entity).ToList())
            GarantirEvento(
                contexto, notificacao.BarbeariaId,
                notificacao.AgendamentoId ?? Guid.NewGuid(), notificacao.EventId);
    }

    private static void GarantirProfissional(ContextoPontoBarber contexto, Guid profissionalId)
    {
        if (!contexto.Set<PerfilProfissional>().Local.Any(perfil => perfil.Id == profissionalId) &&
            !contexto.Set<PerfilProfissional>().IgnoreQueryFilters().Any(perfil => perfil.Id == profissionalId))
            GarantirPerfil(contexto, profissionalId, $"prof-{profissionalId:N}");
    }

    private static void GarantirBarbearia(ContextoPontoBarber contexto, Guid barbeariaId)
    {
        if (contexto.Set<Barbearia>().Local.Any(barbearia => barbearia.Id == barbeariaId) || contexto.Set<Barbearia>().IgnoreQueryFilters().Any(barbearia => barbearia.Id == barbeariaId))
            return;
        var barbearia = Barbearia.Criar(
            "Barbearia Teste", "(11) 99999-9999", "contato@teste.com", "São Paulo",
            "Rua Central, 100", null, $"C{barbeariaId:N}"[..8], "America/Sao_Paulo", Guid.NewGuid());
        barbearia.Id = barbeariaId;
        contexto.Set<Barbearia>().Add(barbearia);
        GarantirProfissional(contexto, barbearia.IdProfissionalProprietario);
    }

    public static void GarantirUsuario(ContextoPontoBarber contexto, string usuarioId)
    {
        if (contexto.Set<Usuario>().Local.Any(usuario => usuario.Id == usuarioId) || contexto.Set<Usuario>().IgnoreQueryFilters().Any(usuario => usuario.Id == usuarioId))
            return;

        contexto.Set<Usuario>().Add(new Usuario
        {
            Id = usuarioId,
            UserName = usuarioId,
            NormalizedUserName = usuarioId.ToUpperInvariant(),
            Email = $"{usuarioId}@teste.local",
            NormalizedEmail = $"{usuarioId.ToUpperInvariant()}@TESTE.LOCAL"
        });
    }

    public static void GarantirPerfil(
        ContextoPontoBarber contexto, Guid profissionalId, string usuarioId)
    {
        GarantirUsuario(contexto, usuarioId);
        if (contexto.Set<PerfilProfissional>().Local.Any(perfil => perfil.Id == profissionalId) || contexto.Set<PerfilProfissional>().IgnoreQueryFilters().Any(perfil => perfil.Id == profissionalId))
            return;

        var perfil = PerfilProfissional.Criar(usuarioId, "Profissional Teste", null);
        perfil.Id = profissionalId;
        contexto.Set<PerfilProfissional>().Add(perfil);
    }

    public static void GarantirVinculo(
        ContextoPontoBarber contexto, Guid barbeariaId, Guid profissionalId)
    {
        GarantirPerfil(contexto, profissionalId, $"prof-{profissionalId:N}");
        if (contexto.Set<VinculoProfissional>().Local.Any(vinculo =>
                vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId) ||
            contexto.Set<VinculoProfissional>().IgnoreQueryFilters().Any(vinculo =>
                vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId))
            return;

        contexto.Set<VinculoProfissional>()
            .Add(VinculoProfissional.CriarBarbeiro(barbeariaId, profissionalId));
    }

    public static Guid GarantirServico(
        ContextoPontoBarber contexto, Guid barbeariaId, Guid? servicoId = null)
    {
        var id = servicoId ?? Guid.NewGuid();
        if (!contexto.Set<Servico>().Local.Any(servico => servico.Id == id) &&
            !contexto.Set<Servico>().IgnoreQueryFilters().Any(servico => servico.Id == id))
        {
            var servico = Servico.Criar(barbeariaId, $"Corte-{id:N}"[..12], 40, 80m);
            servico.Id = id;
            contexto.Set<Servico>().Add(servico);
        }
        return id;
    }

    public static void GarantirAgendamento(
        ContextoPontoBarber contexto,
        Guid barbeariaId,
        Guid agendamentoId,
        Guid profissionalId,
        string clienteId)
    {
        if (contexto.Set<Agendamento>().Local.Any(agendamento => agendamento.Id == agendamentoId) || contexto.Set<Agendamento>().IgnoreQueryFilters().Any(agendamento => agendamento.Id == agendamentoId))
            return;

        var servicoId = GarantirServico(contexto, barbeariaId);
        GarantirVinculo(contexto, barbeariaId, profissionalId);
        var agendamento = Agendamento.Criar(
            barbeariaId, clienteId, profissionalId, servicoId, DateTime.UtcNow.AddDays(1),
            "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        agendamento.Id = agendamentoId;
        contexto.Set<Agendamento>().Add(agendamento);
    }

    public static void GarantirEvento(
        ContextoPontoBarber contexto,
        Guid barbeariaId,
        Guid agendamentoId,
        Guid eventoId)
    {
        if (contexto.Set<HistoricoAgendamento>().Local.Any(evento => evento.Id == eventoId) || contexto.Set<HistoricoAgendamento>().IgnoreQueryFilters().Any(evento => evento.Id == eventoId))
            return;

        GarantirAgendamento(contexto, barbeariaId, agendamentoId, Guid.NewGuid(), "cliente-teste");
        var evento = HistoricoAgendamento.Criar(
            barbeariaId, agendamentoId, TipoEventoHistorico.Criacao, "sistema", DateTime.UtcNow);
        evento.Id = eventoId;
        contexto.Set<HistoricoAgendamento>().Add(evento);
    }
}
