using Microsoft.Data.SqlClient;
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

    private static bool EhConflito(DbUpdateException excecao, string indice) =>
        excecao.InnerException is SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627) &&
        sql.Message.Contains(indice, StringComparison.Ordinal);
}
