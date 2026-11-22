import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import {
  type Barbearia,
  type CriarBarbearia,
  type Profissional,
  type Vinculo,
} from './barbearia.models';

/** Estado e acesso aos endpoints de barbearia e profissionais (Tasks 4.1 a 4.4). */
@Injectable({ providedIn: 'root' })
export class BarbeariaService {
  private readonly http = inject(HttpClient);

  private readonly barbeariaState = signal<Barbearia | null>(null);
  private readonly barbeariaIdState = signal<string | null>(null);
  private readonly vinculoState = signal<Vinculo | null>(null);
  private readonly profissionaisState = signal<Profissional[]>([]);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);

  readonly barbearia = this.barbeariaState.asReadonly();
  readonly barbeariaId = this.barbeariaIdState.asReadonly();
  readonly vinculo = this.vinculoState.asReadonly();
  readonly profissionais = this.profissionaisState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();

  criar(dados: CriarBarbearia): void {
    this.executar(
      () =>
        this.http.post<Barbearia>('/api/v1/barbearias', dados, {
          withCredentials: true,
        }),
      (resposta) => {
        this.barbeariaState.set(resposta);
        this.barbeariaIdState.set(resposta.id);
        this.profissionaisState.set([]);
      },
    );
  }

  entrar(codigoConvite: string): void {
    this.executar(
      () =>
        this.http.post<Vinculo>(
          '/api/v1/barbearias/entrar',
          { codigoConvite },
          { withCredentials: true },
        ),
      (resposta) => {
        this.vinculoState.set(resposta);
        this.barbeariaIdState.set(resposta.barbeariaId);
        this.listarProfissionais(resposta.barbeariaId);
      },
    );
  }

  selecionar(barbeariaId: string): void {
    this.barbeariaIdState.set(barbeariaId);
  }

  obter(barbeariaId: string): void {
    this.executar(
      () =>
        this.http.get<Barbearia>(`/api/v1/barbearias/${barbeariaId}`, {
          withCredentials: true,
        }),
      (resposta) => {
        // O código de convite só é devolvido ao proprietário (Task 4.2).
        this.barbeariaState.set(resposta);
        this.barbeariaIdState.set(barbeariaId);
      },
    );
  }

  listarProfissionais(barbeariaId = this.barbeariaIdState()): void {
    if (!barbeariaId) return;

    this.executar(
      () =>
        this.http.get<Profissional[]>(`/api/v1/barbearias/${barbeariaId}/profissionais`, {
          withCredentials: true,
        }),
      (resposta) => this.profissionaisState.set(resposta),
    );
  }

  atualizarDados(
    barbeariaId: string,
    profissionalId: string,
    dados: { nomeCompleto: string; telefone: string | null },
  ): void {
    this.executar(
      () =>
        this.http.put(`/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}`, dados, {
          withCredentials: true,
        }),
      () => this.listarProfissionais(barbeariaId),
    );
  }

  alterarStatus(barbeariaId: string, profissionalId: string, ativo: boolean): void {
    this.executar(
      () =>
        this.http.patch(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/status`,
          { ativo },
          { withCredentials: true },
        ),
      () => this.listarProfissionais(barbeariaId),
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
