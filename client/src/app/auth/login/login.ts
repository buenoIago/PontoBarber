import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AuthService } from '../auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-login',
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);

  readonly voltar = output<void>();
  readonly email = signal('');
  readonly senha = signal('');

  alterarEmail(evento: Event): void {
    this.email.set((evento.target as HTMLInputElement).value);
  }

  alterarSenha(evento: Event): void {
    this.senha.set((evento.target as HTMLInputElement).value);
  }

  entrar(): void {
    this.auth.entrar({ email: this.email(), senha: this.senha() });
  }
}
