import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { ErroApi, mensagemCompreensivel, ProblemaApi } from './erro-api';

/** Converte erros HTTP em ErroApi com mensagem compreensível e rastreável. */
export const interceptorErro: HttpInterceptorFn = (requisicao, proximo) =>
  proximo(requisicao).pipe(
    catchError((erro: unknown) => {
      if (!(erro instanceof HttpErrorResponse)) return throwError(() => erro);

      const problema = erro.error as ProblemaApi | undefined;
      return throwError(
        () =>
          new ErroApi(
            erro.status,
            problema?.code ?? `http.${erro.status}`,
            problema?.traceId,
            problema?.errors ?? {},
            mensagemCompreensivel(problema, erro.status),
          ),
      );
    }),
  );
