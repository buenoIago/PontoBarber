import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { Button } from '../shared/ui/button/button';
import { EmptyState } from '../shared/ui/empty-state/empty-state';
import { FormField } from '../shared/ui/form-field/form-field';
import { Table } from '../shared/ui/table/table';
import { ClientesService } from './clientes.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-clientes',
  templateUrl: './clientes.html',
})
export class Clientes implements OnInit {
  private readonly servico = inject(ClientesService);
  private readonly barbearias = inject(BarbeariaService);

  readonly itens = this.servico.itens;
  readonly selecionado = this.servico.selecionado;
  readonly total = this.servico.total;
  readonly pagina = this.servico.pagina;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;

  readonly busca = signal('');

  ngOnInit(): void {
    this.servico.limpar();
    this.carregar();
  }

  pesquisar(): void {
    this.carregar(1);
  }

  proxima(): void {
    if (this.pagina() * 20 < this.total()) this.carregar(this.pagina() + 1);
  }

  anterior(): void {
    if (this.pagina() > 1) this.carregar(this.pagina() - 1);
  }

  ver(id: string): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (barbeariaId) this.servico.obter(barbeariaId, id);
  }

  private carregar(pagina = this.pagina()): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (barbeariaId) this.servico.listar(barbeariaId, this.busca(), pagina);
  }
}
