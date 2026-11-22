import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { Table } from '../../shared/ui/table/table';
import { DIAS_SEMANA, type JanelaDisponibilidade } from '../configuracoes.models';
import { ConfiguracoesService } from '../configuracoes.service';

interface JanelaFormulario {
  diaSemana: number;
  iniciaEm: string;
  terminaEm: string;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-disponibilidade-config',
  templateUrl: './disponibilidade.html',
})
export class Disponibilidade implements OnInit {
  private readonly servico = inject(ConfiguracoesService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly dias = DIAS_SEMANA;
  readonly profissionais = this.barbearias.profissionais;
  readonly janelas = this.servico.disponibilidade;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly sucesso = this.servico.sucesso;

  readonly eProprietario = computed(
    () =>
      this.auth.conta()?.tipo === 'Professional' &&
      this.barbearias.barbearia()?.codigoConvite != null,
  );
  readonly eBarbeiro = computed(
    () => this.auth.conta()?.tipo === 'Professional' && !this.eProprietario(),
  );
  readonly profissionalId = computed(() =>
    this.eProprietario()
      ? this.selecionadoId()
      : (this.barbearias.vinculo()?.profissionalId ?? null),
  );
  readonly podeEditar = computed(() => this.profissionalId() !== null);

  readonly selecionadoId = signal<string | null>(null);
  readonly formulario = signal<JanelaFormulario[]>([]);

  constructor() {
    effect(() => {
      const id = this.profissionalId();
      const barbeariaId = this.barbearias.barbeariaId();
      if (!id || !barbeariaId) return;

      this.servico.listarDisponibilidade(barbeariaId, id);
    });

    effect(() => {
      const janelas = this.janelas();
      this.formulario.set(
        DIAS_SEMANA.map((dia) => {
          const janela = janelas.find((item) => item.diaSemana === dia.numero);
          return {
            diaSemana: dia.numero,
            iniciaEm: janela ? janela.iniciaEm.slice(0, 5) : '',
            terminaEm: janela ? janela.terminaEm.slice(0, 5) : '',
          };
        }),
      );
    });
  }

  ngOnInit(): void {
    this.servico.limparMensagens();
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    if (!this.barbearias.barbearia()) this.barbearias.obter(barbeariaId);
    this.barbearias.listarProfissionais(barbeariaId);
    this.selecionadoId.set(this.profissionais()[0]?.id ?? null);
  }

  selecionar(id: string): void {
    this.servico.limparMensagens();
    this.selecionadoId.set(id);
  }

  definirHorario(diaSemana: number, campo: 'iniciaEm' | 'terminaEm', valor: string): void {
    this.formulario.update((janelas) =>
      janelas.map((janela) =>
        janela.diaSemana === diaSemana ? { ...janela, [campo]: valor } : janela,
      ),
    );
  }

  salvar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const profissionalId = this.profissionalId();
    if (!barbeariaId || !profissionalId) return;

    const janelas: JanelaDisponibilidade[] = this.formulario()
      .filter((janela) => janela.iniciaEm && janela.terminaEm)
      .map((janela) => ({
        diaSemana: janela.diaSemana,
        iniciaEm: `${janela.iniciaEm.slice(0, 5)}:00`,
        terminaEm: `${janela.terminaEm.slice(0, 5)}:00`,
      }));

    this.servico.limparMensagens();
    this.servico.salvarDisponibilidade(barbeariaId, profissionalId, janelas);
  }
}
