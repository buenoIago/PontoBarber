using Npgsql;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Profissionais;

/// <summary>
/// Persistência de perfis e vínculos profissionais. As consultas de
/// autorização usam <c>IgnoreQueryFilters</c> de forma justificada: o tenant
/// é justamente o valor sendo validado e ainda não está no contexto da
/// requisição. O predicato por BarbeariaId é sempre explícito.
/// </summary>
public sealed class RepositorioProfissionalEmOrm(ContextoPontoBarber contexto) : IProfissionalRepositorio
{
    public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
        contexto.Set<PerfilProfissional>()
            .AsNoTracking()
            .SingleOrDefaultAsync(perfil => perfil.UsuarioId == usuarioId, cancellationToken);

    public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken cancellationToken) =>
        // Acompanhado: a edição passa por PerfilProfissional.Atualizar e é
        // persistida por SalvarAlteracoesAsync.
        contexto.Set<PerfilProfissional>()
            .SingleOrDefaultAsync(perfil => perfil.Id == profissionalId, cancellationToken);

    public Task<VinculoProfissional?> ObterVinculoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken) =>
        contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                vinculo => vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId,
                cancellationToken);

    public Task<bool> PossuiVinculoAtivoAsync(
        Guid usuarioId,
        Guid barbeariaId,
        CancellationToken cancellationToken) =>
        (from vinculo in contexto.Set<VinculoProfissional>().IgnoreQueryFilters()
         join perfil in contexto.Set<PerfilProfissional>() on vinculo.ProfissionalId equals perfil.Id
         where vinculo.BarbeariaId == barbeariaId &&
               perfil.UsuarioId == usuarioId.ToString() &&
               vinculo.Situacao == StatusVinculo.Ativo
         select vinculo).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<VinculoProfissional>> ListarVinculosAtivosPorUsuarioAsync(
        string usuarioId,
        CancellationToken cancellationToken) =>
        await (from vinculo in contexto.Set<VinculoProfissional>().IgnoreQueryFilters()
               join perfil in contexto.Set<PerfilProfissional>()
                   on vinculo.ProfissionalId equals perfil.Id
               where perfil.UsuarioId == usuarioId &&
                     vinculo.Situacao == StatusVinculo.Ativo
               select vinculo).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken)
    {
        // A consulta também é usada por rotas públicas sem tenant no contexto;
        // o predicato por BarbeariaId é explícito e garante o isolamento.
        var vinculos = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .Where(vinculo => vinculo.BarbeariaId == barbeariaId && vinculo.Situacao != StatusVinculo.Removido)
            .ToListAsync(cancellationToken);

        var ids = vinculos.Select(vinculo => vinculo.ProfissionalId).ToList();
        var perfis = await contexto.Set<PerfilProfissional>()
            .Where(perfil => ids.Contains(perfil.Id))
            .ToDictionaryAsync(perfil => perfil.Id, cancellationToken);

        return vinculos
            .Where(vinculo => perfis.ContainsKey(vinculo.ProfissionalId))
            .Select(vinculo => new ProfissionalVinculo(perfis[vinculo.ProfissionalId], vinculo))
            .ToList();
    }

    public async Task AdicionarVinculoAsync(
        PerfilProfissional? perfilNovo,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken)
    {
        if (perfilNovo is not null)
            contexto.Set<PerfilProfissional>().Add(perfilNovo);

        contexto.Set<VinculoProfissional>().Add(vinculo);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhConflito(excecao, "UX_Vinculos_Barbearia_Profissional"))
        {
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.TenantConviteConflito,
                "O profissional já possui vínculo com esta barbearia.");
        }
        catch (DbUpdateException excecao) when (EhConflito(excecao, "UX_Profissionais_UsuarioId"))
        {
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.TenantConviteConflito,
                "O perfil profissional já existe para este usuário.");
        }
    }

    public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken) =>
        // Mesma justificativa de ObterVinculoAsync: o tenant sendo validado é
        // o da rota e ainda não está no contexto da requisição. O predicato
        // por BarbeariaId é explícito e a entidade volta acompanhada.
        contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                vinculo => vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId,
                cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        contexto.SaveChangesAsync(cancellationToken);

    private static bool EhConflito(DbUpdateException excecao, string indice) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == "23505" &&
        postgres.MessageText.Contains(indice, StringComparison.Ordinal);
}
