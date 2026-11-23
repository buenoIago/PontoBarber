import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import type { RegistroCliente, RegistroProfissional } from '../auth.models';
import { AuthService } from '../auth.service';
import type { TipoPerfil } from '../escolha-perfil/escolha-perfil';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-cadastro',
  templateUrl: './cadastro.html',
})
export class Cadastro {
  private readonly auth = inject(AuthService);

  readonly perfil = input.required<TipoPerfil>();
  readonly voltar = output<void>();
  readonly entrar = output<void>();

  readonly nomeCompleto = signal('');
  readonly email = signal('');
  readonly telefone = signal('');
  readonly senha = signal('');
  readonly confirmarSenha = signal('');

  readonly eProfissional = () => this.perfil() === 'profissional';

  enviar(): void {
    if (this.eProfissional()) {
      const dados: RegistroProfissional = {
        nomeCompleto: this.nomeCompleto().trim(),
        email: this.email().trim(),
        telefone: this.telefone().trim(),
        senha: this.senha(),
        confirmarSenha: this.confirmarSenha(),
      };
      this.auth.cadastrarProfissional(dados);
      return;
    }

    const dados: RegistroCliente = {
      nomeCompleto: this.nomeCompleto().trim(),
      email: this.email().trim(),
      telefone: this.telefone().trim(),
    };
    this.auth.cadastrarCliente(dados);
  }
}
