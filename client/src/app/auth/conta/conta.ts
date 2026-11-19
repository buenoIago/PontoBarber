import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

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
  readonly senhaAtual = signal('');
  readonly novaSenha = signal('');
  readonly confirmarNovaSenha = signal('');

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
