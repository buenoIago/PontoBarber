import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import {
  type Bloqueio,
  type DiaFuncionamento,
  type JanelaDisponibilidade,
} from './configuracoes.models';

/** Estado e acesso aos endpoints de configuração de horários e bloqueios. */
@Injectable({ providedIn: 'root' })
export class ConfiguracoesService {
  private readonly http = inject(HttpClient);

  private readonly horariosState = signal<DiaFuncionamento[]>([]);
  private readonly disponibilidadeState = signal<JanelaDisponibilidade[]>([]);
  private readonly bloqueiosState = signal<Bloqueio[]>([]);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);
  private readonly sucessoState = signal<string | null>(null);

  readonly horarios = this.horariosState.asReadonly();
  readonly disponibilidade = this.disponibilidadeState.asReadonly();
  readonly bloqueios = this.bloqueiosState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();
  readonly sucesso = this.sucessoState.asReadonly();

  listarHorarios(barbeariaId: string): void {
    this.executar(
      () =>
        this.http.get<DiaFuncionamento[]>(
          `/api/v1/barbearias/${barbeariaId}/horarios-funcionamento`,
          { withCredentials: true },
        ),
      (resposta) => this.horariosState.set(resposta),
    );
  }

  salvarHorarios(barbeariaId: string, dias: DiaFuncionamento[], sucesso?: () => void): void {
    this.executar(
      () =>
        this.http.put(`/api/v1/barbearias/${barbeariaId}/horarios-funcionamento`, dias, {
          withCredentials: true,
        }),
      (resposta) => {
        this.horariosState.set(resposta as DiaFuncionamento[]);
        this.sucessoState.set('Horários salvos.');
        sucesso?.();
      },
    );
  }

  listarDisponibilidade(barbeariaId: string, profissionalId: string): void {
    this.executar(
      () =>
        this.http.get<JanelaDisponibilidade[]>(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/disponibilidade`,
          { withCredentials: true },
        ),
      (resposta) => this.disponibilidadeState.set(resposta),
    );
  }

  salvarDisponibilidade(
    barbeariaId: string,
    profissionalId: string,
    janelas: JanelaDisponibilidade[],
    sucesso?: () => void,
  ): void {
    this.executar(
      () =>
        this.http.put(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/disponibilidade`,
          janelas,
          { withCredentials: true },
        ),
      (resposta) => {
        this.disponibilidadeState.set(resposta as JanelaDisponibilidade[]);
        this.sucessoState.set('Disponibilidade salva.');
        sucesso?.();
      },
    );
  }

  listarBloqueios(barbeariaId: string, profissionalId: string): void {
    this.executar(
      () =>
        this.http.get<Bloqueio[]>(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/blocks`,
          { withCredentials: true },
        ),
      (resposta) => this.bloqueiosState.set(resposta),
    );
  }

  criarBloqueio(
    barbeariaId: string,
    profissionalId: string,
    dados: { iniciaEmUtc: string; terminaEmUtc: string; motivo: string | null },
    sucesso?: () => void,
  ): void {
    this.executar(
      () =>
        this.http.post(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/blocks`,
          dados,
          { withCredentials: true },
        ),
      () => {
        this.sucessoState.set('Bloqueio adicionado.');
        this.listarBloqueios(barbeariaId, profissionalId);
        sucesso?.();
      },
    );
  }

  removerBloqueio(
    barbeariaId: string,
    profissionalId: string,
    bloqueioId: string,
    sucesso?: () => void,
  ): void {
    this.executar(
      () =>
        this.http.delete(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/blocks/${bloqueioId}`,
          { withCredentials: true },
        ),
      () => {
        this.sucessoState.set('Bloqueio removido.');
        this.listarBloqueios(barbeariaId, profissionalId);
        sucesso?.();
      },
    );
  }

  limparMensagens(): void {
    this.erroState.set(null);
    this.sucessoState.set(null);
  }

  private executar<T>(operacao: () => Observable<T>, concluido: (resposta: T) => void): void {
    this.erroState.set(null);
    this.carregandoState.set(true);

    operacao()
      .pipe(finalize(() => this.carregandoState.set(false)))
      .subscribe({
        next: (resposta) => concluido(resposta),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível concluir a operação.',
          ),
      });
  }
}
