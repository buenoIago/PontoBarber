import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AuthService } from '../auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-verificacao-otp',
  templateUrl: './verificacao-otp.html',
})
export class VerificacaoOtp {
  private readonly auth = inject(AuthService);

  readonly voltar = output<void>();

  readonly email = this.auth.emailOtp;
  readonly carregando = this.auth.carregando;
  readonly codigo = signal('');

  alterarCodigo(evento: Event): void {
    this.codigo.set((evento.target as HTMLInputElement).value.replace(/\D/g, '').slice(0, 6));
  }

  verificar(): void {
    const email = this.email();
    if (!email) return;
    this.auth.verificarCodigo({ email, codigo: this.codigo() });
  }

  reenviar(): void {
    const email = this.email();
    if (!email) return;
    this.codigo.set('');
    this.auth.solicitarCodigo(email);
  }

  cancelar(): void {
    this.auth.cancelarOtp();
    this.voltar.emit();
  }
}
