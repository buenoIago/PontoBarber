import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, type Observable } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import {
  type Agendamento,
  type BloqueioAgenda,
  type Disponibilidade,
  type NovoAgendamento,
  type ServicoDisponivel,
  type Slot,
} from './agendamentos.models';

/** Acesso aos endpoints de agendamento; preço e duração vêm do servidor. */
@Injectable({ providedIn: 'root' })
export class AgendamentosService {
  private readonly http = inject(HttpClient);

  private readonly agendaState = signal<Agendamento[]>([]);
  private readonly bloqueiosState = signal<BloqueioAgenda[]>([]);
  private readonly servicosState = signal<ServicoDisponivel[]>([]);
  private readonly slotsState = signal<Slot[]>([]);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);
  private readonly sucessoState = signal<string | null>(null);

  readonly agenda = this.agendaState.asReadonly();
  readonly bloqueios = this.bloqueiosState.asReadonly();
  readonly servicos = this.servicosState.asReadonly();
  readonly slots = this.slotsState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();
  readonly sucesso = this.sucessoState.asReadonly();

  listarAgenda(
    barbeariaId: string,
    de: string,
    ate: string,
    profissionalId?: string | null,
  ): void {
    const filtro = profissionalId ? `&profissionalId=${profissionalId}` : '';
    this.executar(
      () =>
        this.http.get<Agendamento[]>(
          `/api/v1/barbearias/${barbeariaId}/agendamentos?de=${de}&ate=${ate}${filtro}`,
          { withCredentials: true },
        ),
      (resposta) => this.agendaState.set(resposta),
    );
  }

  listarBloqueios(barbeariaId: string, profissionalId: string): void {
    this.executar(
      () =>
        this.http.get<BloqueioAgenda[]>(
          `/api/v1/barbearias/${barbeariaId}/profissionais/${profissionalId}/blocks`,
          { withCredentials: true },
        ),
      (resposta) => this.bloqueiosState.set(resposta),
    );
  }

  listarServicos(barbeariaId: string): void {
    this.executar(
      () =>
        this.http.get<ServicoDisponivel[]>(`/api/v1/barbearias/${barbeariaId}/servicos`, {
          withCredentials: true,
        }),
      (resposta) => this.servicosState.set(resposta.filter((servico) => servico.ativo)),
    );
  }

  consultarSlots(
    barbeariaId: string,
    servicoId: string,
    profissionalId: string,
    data: string,
  ): void {
    this.executar(
      () =>
        this.http.get<Disponibilidade>(
          `/api/v1/barbearias/${barbeariaId}/disponibilidade?servicoId=${servicoId}&profissionalId=${profissionalId}&date=${data}&dias=1`,
          { withCredentials: true },
        ),
      (resposta) => this.slotsState.set(resposta.slots),
    );
  }

  confirmar(
    barbeariaId: string,
    dados: NovoAgendamento,
    publico: boolean,
    sucesso?: () => void,
  ): void {
    const rota = publico
      ? `/api/v1/barbearias/${barbeariaId}/agendamentos/public`
      : `/api/v1/barbearias/${barbeariaId}/agendamentos`;

    this.executar(
      () => this.http.post(rota, dados, { withCredentials: true }),
      () => {
        this.sucessoState.set('Agendamento confirmado.');
        sucesso?.();
      },
    );
  }

  listarMeus(de: string, ate: string): void {
    this.executar(
      () =>
        this.http.get<Agendamento[]>(`/api/v1/me/agendamentos?de=${de}&ate=${ate}`, {
          withCredentials: true,
        }),
      (resposta) => this.agendaState.set(resposta),
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
