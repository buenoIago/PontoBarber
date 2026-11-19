import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { Cadastro } from './cadastro';

@Component({
  imports: [Cadastro],
  template: ` <app-cadastro perfil="cliente" (voltar)="voltar.set(true)" /> `,
})
class Host {
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

  async function preencher(elemento: HTMLElement): Promise<void> {
    const valores: Record<string, string> = {
      nome: 'Nome Teste',
      email: 'novo@exemplo.com',
      telefone: '11999999999',
      senha: 'Senha123',
      confirmarSenha: 'Senha123',
    };

    for (const [id, valor] of Object.entries(valores)) {
      const campo = elemento.querySelector<HTMLInputElement>(`input#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input', { bubbles: true }));
      fixture.detectChanges();
    }
    await fixture.whenStable();
  }

  it('should post to the customer endpoint with the typed data', async () => {
    const elemento = await criar();
    await preencher(elemento);

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const requisicao = http.expectOne('/api/v1/auth/register/customer');
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body.email).toBe('novo@exemplo.com');
    requisicao.flush(null, { status: 201, statusText: 'Created' });
    http.expectOne('/api/v1/auth/me').flush(null);
    http.verify();
  });

  it('should emit voltar when requested', async () => {
    const elemento = await criar();
    elemento.querySelector<HTMLButtonElement>('button.btn-link')?.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.voltar()).toBe(true);
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
