import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { Cadastro } from './cadastro';

@Component({
  imports: [Cadastro],
  template: ` <app-cadastro [perfil]="perfil()" (voltar)="voltar.set(true)" /> `,
})
class Host {
  readonly perfil = signal<'cliente' | 'profissional'>('cliente');
  readonly voltar = signal(false);
}

describe('Cadastro', () => {
  let fixture: ComponentFixture<Host>;
  let http: HttpTestingController;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [Host],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Host);
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  async function preencher(elemento: HTMLElement, profissional = false): Promise<void> {
    const valores: Record<string, string> = {
      nome: 'Nome Teste',
      email: 'novo@exemplo.com',
      telefone: '11999999999',
    };
    if (profissional) {
      valores['senha'] = 'Senha123';
      valores['confirmarSenha'] = 'Senha123';
    }

    for (const [id, valor] of Object.entries(valores)) {
      const campo = elemento.querySelector<HTMLInputElement>(`input#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input', { bubbles: true }));
      fixture.detectChanges();
    }
    await fixture.whenStable();
  }

  it('should register a customer without password fields', async () => {
    const elemento = await criar();
    expect(elemento.querySelector('input#senha')).toBeNull();
    expect(elemento.querySelector('input#confirmarSenha')).toBeNull();
    await preencher(elemento);

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const requisicao = http.expectOne('/api/v1/auth/cadastro/cliente');
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual({
      nomeCompleto: 'Nome Teste',
      email: 'novo@exemplo.com',
      telefone: '11999999999',
    });
    requisicao.flush(null, { status: 201, statusText: 'Created' });
    http.verify();
  });

  it('should keep password fields for professionals', async () => {
    const elemento = await criar();
    fixture.componentInstance.perfil.set('profissional');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(elemento.querySelector('input#senha')).not.toBeNull();
    expect(elemento.querySelector('input#confirmarSenha')).not.toBeNull();
    await preencher(elemento, true);

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const requisicao = http.expectOne('/api/v1/auth/cadastro/profissional');
    expect(requisicao.request.body.senha).toBe('Senha123');
    requisicao.flush(null, { status: 201, statusText: 'Created' });
    http.verify();
  });

  it('should emit voltar when requested', async () => {
    const elemento = await criar();
    const botaoVoltar = [...elemento.querySelectorAll<HTMLButtonElement>('button.btn-link')].find(
      (botao) => botao.textContent?.trim() === 'Voltar',
    );
    botaoVoltar?.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.voltar()).toBe(true);
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
