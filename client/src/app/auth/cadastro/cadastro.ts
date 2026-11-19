import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
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

  readonly nomeCompleto = signal('');
  readonly email = signal('');
  readonly telefone = signal('');
  readonly senha = signal('');
  readonly confirmarSenha = signal('');

  enviar(): void {
    const dados = {
      nomeCompleto: this.nomeCompleto(),
      email: this.email(),
      telefone: this.telefone(),
      senha: this.senha(),
      confirmarSenha: this.confirmarSenha(),
    };

    if (this.perfil() === 'cliente') this.auth.cadastrarCliente(dados);
    else this.auth.cadastrarProfissional(dados);
  }
}
