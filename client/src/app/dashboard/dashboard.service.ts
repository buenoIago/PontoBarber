import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import { type DashboardResumo } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  private readonly resumoState = signal<DashboardResumo | null>(null);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);

  readonly resumo = this.resumoState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();

  carregar(barbeariaId: string): void {
    this.erroState.set(null);
    this.carregandoState.set(true);

    this.http
      .get<DashboardResumo>(`/api/v1/barbearias/${barbeariaId}/dashboard`, {
        withCredentials: true,
      })
      .pipe(finalize(() => this.carregandoState.set(false)))
      .subscribe({
        next: (resposta) => this.resumoState.set(resposta),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível carregar o dashboard.',
          ),
      });
  }
}
