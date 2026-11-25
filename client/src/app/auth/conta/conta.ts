import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AuthService } from '../auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-conta',
  templateUrl: './conta.html',
})
export class Conta {
  private readonly auth = inject(AuthService);

  readonly conta = this.auth.conta;
  readonly erro = this.auth.erro;
  readonly sucesso = this.auth.sucesso;
  readonly carregando = this.auth.carregando;
  readonly eCliente = computed(() => this.auth.conta()?.tipo === 'Customer');

  readonly nome = signal('');
  readonly email = signal('');
  readonly telefone = signal('');
  readonly senhaAtual = signal('');
  readonly novaSenha = signal('');
  readonly confirmarNovaSenha = signal('');

  constructor() {
    effect(() => {
      const conta = this.conta();
      if (!conta) return;
      this.nome.set(conta.nomeCompleto);
      this.email.set(conta.email);
      this.telefone.set(conta.telefone ?? '');
    });
  }

  salvarDados(): void {
    this.auth.atualizarPerfilCliente({
      nomeCompleto: this.nome().trim(),
      email: this.email().trim(),
      telefone: this.telefone().trim() || null,
    });
  }

  alterarSenha(): void {
    this.auth.alterarSenha({
      senhaAtual: this.senhaAtual(),
      novaSenha: this.novaSenha(),
      confirmarNovaSenha: this.confirmarNovaSenha(),
    });
  }

  sair(): void {
    this.auth.sair();
  }
}
