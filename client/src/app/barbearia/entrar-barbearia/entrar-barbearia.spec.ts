import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { EntrarBarbearia } from './entrar-barbearia';

describe('EntrarBarbearia', () => {
  let fixture: ComponentFixture<EntrarBarbearia>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EntrarBarbearia],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EntrarBarbearia);
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  it('should send the invite code and show the resulting role', async () => {
    const elemento = fixture.nativeElement as HTMLElement;
    const codigo = elemento.querySelector<HTMLInputElement>('input#convite-codigo')!;
    codigo.value = 'ABC23456';
    codigo.dispatchEvent(new Event('input', { bubbles: true }));

    fixture.detectChanges();
    await fixture.whenStable();

    elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const requisicao = http.expectOne('/api/v1/barbearias/entrar');
    expect(requisicao.request.body).toEqual({ codigoConvite: 'ABC23456' });
    requisicao.flush({
      id: 'v1',
      barbeariaId: 'b1',
      profissionalId: 'p1',
      cargo: 'Barbeiro',
      situacao: 'Ativo',
    });
    http.expectOne('/api/v1/barbearias/b1/profissionais').flush([]);
    await fixture.whenStable();

    expect(elemento.textContent).toContain('Barbeiro');
    http.verify();
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(fixture.nativeElement as HTMLElement)).toEqual([]);
  });
});
