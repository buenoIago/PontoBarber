import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { Table } from '../../shared/ui/table/table';
import { type Servico } from '../servicos.models';
import { ServicosService } from '../servicos.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-catalogo-servicos',
  templateUrl: './catalogo.html',
})
export class CatalogoServicos {
  private readonly servicos = inject(ServicosService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly barbeariaId = this.barbearias.barbeariaId;
  readonly barbearia = this.barbearias.barbearia;
  readonly servicosDaBarbearia = this.servicos.servicos;
  readonly carregando = this.servicos.carregando;
  readonly erro = this.servicos.erro;

  readonly eCliente = computed(() => this.auth.conta()?.tipo === 'Customer');
  readonly eProprietario = computed(
    () => !this.eCliente() && this.barbearia()?.codigoConvite != null,
  );
  readonly eBarbeiro = computed(() => !this.eCliente() && !this.eProprietario());

  readonly sucesso = signal<string | null>(null);
  readonly criando = signal(false);
  readonly editandoId = signal<string | null>(null);
  readonly nome = signal('');
  readonly duracao = signal(0);
  readonly preco = signal(0);
  readonly ativo = signal(true);
  readonly idInformado = signal('');

  constructor() {
    effect(() => {
      const barbeariaId = this.barbeariaId();
      if (barbeariaId) this.servicos.listar(barbeariaId);
    });
  }

  selecionar(): void {
    const id = this.idInformado().trim();
    if (!id) return;

    this.sucesso.set(null);
    this.barbearias.selecionar(id);
  }

  recarregar(): void {
    const barbeariaId = this.barbeariaId();
    if (barbeariaId) this.servicos.listar(barbeariaId);
  }

  iniciarCriacao(): void {
    this.limparFormulario();
    this.criando.set(true);
  }

  iniciarEdicao(servico: Servico): void {
    this.sucesso.set(null);
    this.criando.set(false);
    this.editandoId.set(servico.id);
    this.nome.set(servico.nome);
    this.duracao.set(servico.duracaoMinutos);
    this.preco.set(servico.preco ?? 0);
    this.ativo.set(servico.ativo);
  }

  cancelar(): void {
    this.limparFormulario();
  }

  salvar(): void {
    const barbeariaId = this.barbeariaId();
    const duracao = this.duracao();
    const preco = this.preco();

    if (duracao <= 0 || preco < 0 || (!this.eBarbeiro() && !this.nome().trim())) return;

    const editando = this.editandoId();
    const dados = this.eBarbeiro()
      ? { duracaoMinutos: duracao, preco }
      : { nome: this.nome().trim(), duracaoMinutos: duracao, preco, ativo: this.ativo() };

    if (!barbeariaId) return;
    const concluido = () => {
      this.limparFormulario();
      this.sucesso.set('Serviço salvo com sucesso.');
    };

    if (editando) this.servicos.atualizar(barbeariaId, editando, dados, concluido);
    else this.servicos.criar(barbeariaId, dados, concluido);
  }

  remover(servico: Servico): void {
    const barbeariaId = this.barbeariaId();
    if (!barbeariaId) return;

    this.sucesso.set(null);
    this.servicos.remover(barbeariaId, servico.id, () =>
      this.sucesso.set('Serviço removido da oferta.'),
    );
  }

  precoTexto(servico: Servico): string {
    return servico.preco === null ? 'Não definido' : `R$ ${servico.preco.toFixed(2)}`;
  }

  private limparFormulario(): void {
    this.criando.set(false);
    this.editandoId.set(null);
    this.nome.set('');
    this.duracao.set(0);
    this.preco.set(0);
    this.ativo.set(true);
  }
}
