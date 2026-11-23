using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Clientes;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Modulos.Clientes;

public sealed class RepositorioClienteEmOrm(ContextoPontoBarber contexto) : IClienteRepositorio
{
    private IQueryable<Usuario> QueryUsuarios(Guid barbeariaId, string? busca)
    {
        // O filtro global de tenant e o predicado explícito limitam os IDs
        // aos agendamentos da barbearia autorizada; DISTINCT evita duplicatas.
        var clienteIds = contexto.Set<Agendamento>()
            .Where(agendamento => agendamento.BarbeariaId == barbeariaId)
            .Select(agendamento => agendamento.ClienteId)
            .Distinct();

        var query = contexto.Set<Usuario>()
            .Where(usuario => clienteIds.Contains(usuario.Id) &&
                              usuario.UserType == ServicoConta.TipoCliente);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(usuario =>
                usuario.FullName.StartsWith(termo) ||
                (usuario.Email ?? string.Empty).StartsWith(termo));
        }

        return query;
    }

    private static ClienteResumo Projecao(Usuario usuario) =>
        new(usuario.Id, usuario.FullName, usuario.Email ?? string.Empty, usuario.PhoneNumber);

    public async Task<IReadOnlyList<ClienteResumo>> ListarAsync(
        Guid barbeariaId,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken) =>
        await QueryUsuarios(barbeariaId, busca)
            .OrderBy(usuario => usuario.FullName)
            .ThenBy(usuario => usuario.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(usuario => new ClienteResumo(
                usuario.Id, usuario.FullName, usuario.Email ?? string.Empty, usuario.PhoneNumber))
            .ToListAsync(cancellationToken);

    public Task<int> ContarAsync(
        Guid barbeariaId,
        string? busca,
        CancellationToken cancellationToken) =>
        QueryUsuarios(barbeariaId, busca).CountAsync(cancellationToken);

    public async Task<ClienteResumo?> ObterAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var usuario = await QueryUsuarios(barbeariaId, null)
            .FirstOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        return usuario is null ? null : Projecao(usuario);
    }
}
