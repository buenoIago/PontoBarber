import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AuthService } from '../auth.service';
import type { TipoPerfil } from '../escolha-perfil/escolha-perfil';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-login',
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);

  readonly perfil = input.required<TipoPerfil>();
  readonly voltar = output<void>();

  readonly email = signal('');
  readonly senha = signal('');

  readonly eCliente = () => this.perfil() === 'cliente';

  alterarEmail(evento: Event): void {
    this.email.set((evento.target as HTMLInputElement).value);
  }

  alterarSenha(evento: Event): void {
    this.senha.set((evento.target as HTMLInputElement).value);
  }

  entrar(): void {
    const email = this.email().trim();
    if (this.eCliente()) {
      this.auth.solicitarCodigo(email);
    } else {
      this.auth.entrar({ email, senha: this.senha() });
    }
  }
}
