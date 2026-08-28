declare module 'html2pdf.js' {
  export interface Html2PdfOptions {
    margin?: number | number[] | [number, number] | [number, number, number, number] | any;
    filename?: string;
    image?: { type?: string; quality?: number };
    enableLinks?: boolean;
    html2canvas?: {
      scale?: number;
      useCORS?: boolean;
      logging?: boolean;
      backgroundColor?: string | null;
      [key: string]: any;
    };
    jsPDF?: {
      unit?: string;
      format?: string | number[];
      orientation?: 'portrait' | 'landscape';
      [key: string]: any;
    };
    pagebreak?: {
      mode?: string | string[];
      before?: string | string[];
      after?: string | string[];
      avoid?: string | string[];
      [key: string]: any;
    };
  }

  export interface Html2PdfInstance {
    set(options: Html2PdfOptions): Html2PdfInstance;
    from(element: HTMLElement | string): Html2PdfInstance;
    save(): Promise<void>;
    outputPdf(type?: string): Promise<any>;
    toPdf(): Html2PdfInstance;
    get(type: string): Promise<any>;
  }

  function html2pdf(): Html2PdfInstance;
  export default html2pdf;
}
