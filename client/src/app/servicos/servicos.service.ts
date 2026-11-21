import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import { type DadosServico, type Servico } from './servicos.models';

/** Acesso ao catálogo de serviços. A autorização permanece na API. */
@Injectable({ providedIn: 'root' })
export class ServicosService {
  private readonly http = inject(HttpClient);

  private readonly servicosState = signal<Servico[]>([]);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);

  readonly servicos = this.servicosState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();

  listar(barbeariaId: string, sucesso?: () => void): void {
    this.executar(
      () =>
        this.http.get<Servico[]>(`/api/v1/barbearias/${barbeariaId}/servicos`, {
          withCredentials: true,
        }),
      (resposta) => {
        this.servicosState.set(resposta);
        sucesso?.();
      },
    );
  }

  criar(barbeariaId: string, dados: DadosServico, sucesso?: () => void): void {
    this.executar(
      () =>
        this.http.post(`/api/v1/barbearias/${barbeariaId}/servicos`, dados, {
          withCredentials: true,
        }),
      () => {
        this.listar(barbeariaId, sucesso);
      },
    );
  }

  atualizar(
    barbeariaId: string,
    servicoId: string,
    dados: DadosServico,
    sucesso?: () => void,
  ): void {
    this.executar(
      () =>
        this.http.put(`/api/v1/barbearias/${barbeariaId}/servicos/${servicoId}`, dados, {
          withCredentials: true,
        }),
      () => {
        this.listar(barbeariaId, sucesso);
      },
    );
  }

  remover(barbeariaId: string, servicoId: string, sucesso?: () => void): void {
    this.executar(
      () =>
        this.http.delete(`/api/v1/barbearias/${barbeariaId}/servicos/${servicoId}`, {
          withCredentials: true,
        }),
      () => {
        this.listar(barbeariaId, sucesso);
      },
    );
  }

  limparErro(): void {
    this.erroState.set(null);
  }

  private executar<T>(operacao: () => Observable<T>, sucesso: (resposta: T) => void): void {
    this.erroState.set(null);
    this.carregandoState.set(true);

    operacao()
      .pipe(finalize(() => this.carregandoState.set(false)))
      .subscribe({
        next: (resposta) => sucesso(resposta),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível concluir a operação.',
          ),
      });
  }
}
