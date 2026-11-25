import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import { type Cliente, type ListaClientes } from './clientes.models';

/** Consulta somente leitura: a relação é derivada dos agendamentos. */
@Injectable({ providedIn: 'root' })
export class ClientesService {
  private readonly http = inject(HttpClient);

  private readonly itensState = signal<Cliente[]>([]);
  private readonly selecionadoState = signal<Cliente | null>(null);
  private readonly totalState = signal(0);
  private readonly paginaState = signal(1);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);

  readonly itens = this.itensState.asReadonly();
  readonly selecionado = this.selecionadoState.asReadonly();
  readonly total = this.totalState.asReadonly();
  readonly pagina = this.paginaState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();

  listar(barbeariaId: string, busca: string, pagina: number): void {
    const parametros = new URLSearchParams({ pagina: String(pagina), tamanho: '20' });
    if (busca.trim()) parametros.set('busca', busca.trim());

    this.executar(
      () =>
        this.http.get<ListaClientes>(
          `/api/v1/barbearias/${barbeariaId}/customers?${parametros.toString()}`,
          { withCredentials: true },
        ),
      (resposta) => {
        this.itensState.set(resposta.itens);
        this.totalState.set(resposta.total);
        this.paginaState.set(resposta.pagina);
      },
    );
  }

  obter(barbeariaId: string, id: string): void {
    this.executar(
      () =>
        this.http.get<Cliente>(`/api/v1/barbearias/${barbeariaId}/customers/${id}`, {
          withCredentials: true,
        }),
      (resposta) => this.selecionadoState.set(resposta),
    );
  }

  limpar(): void {
    this.erroState.set(null);
    this.selecionadoState.set(null);
  }

  private executar<T>(operacao: () => Observable<T>, concluido: (resposta: T) => void): void {
    this.erroState.set(null);
    this.carregandoState.set(true);

    operacao()
      .pipe(finalize(() => this.carregandoState.set(false)))
      .subscribe({
        next: concluido,
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível concluir a operação.',
          ),
      });
  }
}
