<template>
    <div class="relative w-full h-full">
        <canvas id="c" :width="w" @mousemove="mousemove" @mouseleave="mouseleave" :height="h"
            class="bg-black w-full h-full absolute">
        </canvas>
        <!-- <div v-if="showPrice" class="absolute text-white select-none bg-black p-3 rounded  pointer-events-none"
            :style="{ left: mousePos.x + 'px', top: mousePos.y + 'px' }">
            <div>Price: {{ selectedData.p }}</div>
            <div>Quantity: {{ selectedData.q }}</div>
        </div> -->
        <div v-show="showPrice" id="details" class="absolute flex flex-gap-4 items-center bg-black/50 rounded p-2 transition duration-300 ease-out group
         select-none pointer-events-none">
            <!-- :style="{ left: mousePos.x + 'px', top: mousePos.y + 'px' }" -->

            <div>
                <p class="p-1 text-xs text-white whitespace-nowrap font-semibold">Price:</p>
                <span class="p-1 text-xs leading-none text-white">${{ selectedData.p }}</span>
            </div>
            <div>
                <p class="p-1 text-xs text-green-500 font-bold">Qty:</p>
                <span class="p-1 text-xs font-medium text-white decoration-gray-600">{{ selectedData.q }}</span>
            </div>
            <br>
            <p class="p-1 text-white">{{ round(1 - baseLine / selectedData.p) }}%</p>
        </div>
        <div class="absolute text-white right-0 
        top-1/2 select-none
        -translate-y-1/2 text-[0.6rem] absolute font-mono" v-text="baseLine">
        </div>
    </div>
</template>

<script lang="ts" setup>

const w = useState("width", () => 75);
const h = useState("height", () => 250);
</script>

<script lang="ts">
import * as signalR from "@microsoft/signalr";
import type OrderBookSnapshot from "./types/OrderBookSnapshot";
import type SymbolInfo from "./types/SymbolInfo";
import type Quote from "./types/Quote";
import ConnectionState from "./types/ConnectionState";

function emptyQuote(p: number): Quote { return ({ p: p, q: 0 }) };
export default {
    data() {
        const { SERVER_URL, FPS } = useRuntimeConfig().public;
        return {
            timer: {} as NodeJS.Timeout,
            connection: {} as signalR.HubConnection,
            imageData: {} as ImageData,
            c: {} as HTMLCanvasElement,
            ctx: undefined as (CanvasRenderingContext2D | undefined),
            isStopped: false,
            snapshots: [] as OrderBookSnapshot[],
            width: 50, // todo this should be in the state and bidirectional
            height: 100, // same as above
            minTick: 0.001, // same ^
            SERVER_URL: SERVER_URL as string,
            FPS: FPS as number,
            baseLine: 0,
            showPrice: false,
            mousePos: { x: 0, y: 0 },
            selectedData: { p: 0, q: 0 },
            detailsElem: {} as HTMLDivElement,
            tickDigits: 3,
            // measure: new Date(),
            // pauseListener: {},
        };
    },
    mounted() {
        const c = document.getElementById("c") as HTMLCanvasElement;
        const ctx = c.getContext("2d") as CanvasRenderingContext2D;

        ctx.imageSmoothingEnabled = false;

        // console.time("hehe");
        this.ctx = ctx;
        this.c = c;
        this.detailsElem = document.getElementById("details") as HTMLDivElement;

        // we do this for drawing speed
        this.imageData = this.ctx.createImageData(1, 1);

        // note in teh future, i might want to scale canvsa to allow drawing or whatever

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${this.SERVER_URL}/ws`)
            .withAutomaticReconnect([1000, 10000, 30000])
            .build();


        this.connection.on('upd', this.onDataReceived);

        const connState = useState<ConnectionState>("connectionState", () => ConnectionState.Disconnected);

        this.connection.onreconnecting(() => {
            connState.value = ConnectionState.Connecting;
        });
        this.connection.onclose(() => {
            connState.value = ConnectionState.Disconnected;
        });
        this.connection.onreconnected(async () => {
            connState.value = ConnectionState.Connected;
        });

        document.addEventListener("keypress", (e) => {
            if (e.code == "Space") {
                this.isStopped = !this.isStopped;
            }
        });

        window.requestAnimationFrame(this.onFrame);

        this.connection.start()
            .then(async () => {
                connState.value = ConnectionState.Connected;
                await this.tryTrack();
            });
    },
    beforeUnmount() {
        this.connection.stop();
        clearInterval(this.timer);
    },
    computed: {
        symbol() {
            return useState("symbol");
        }
    },
    watch: {
        async symbol(oldSymbol, newSymbol) {
            await this.tryTrack()
        }
    },
    methods: {
        onFrame() {

            this.drawAll();
            window.requestAnimationFrame(this.onFrame);
        },
        mousemove(ev: MouseEvent) {
            const rect = this.c.getBoundingClientRect();
            this.showPrice = true;

            this.mousePos = {
                x: ev.clientX - rect.left,
                y: ev.clientY - rect.top,
            }
        },
        mouseleave() {
            this.showPrice = false;
        },
        async tryTrack() {
            const newSymbol = this.symbol.value ?? "ordiusdt";
            const res = await fetch(`${this.SERVER_URL}/symbol?name=${newSymbol}`)
            const data = await res.json() as SymbolInfo;
            if (data !== null) {
                this.snapshots = [];
                this.minTick = data.tickSize;

                this.tickDigits = this.minTick % 1 == 0 ? 0 : this.minTick.toString().split(".")[1].length;

                useState("tickSize").value = this.minTick;

                this.ctx?.clearRect(0, 0, this.c.width, this.c.height);
                this.connection.send("sub", newSymbol);
            }
        },
        drawAll() {
            // draw the last width worth of snapshots

            const width = useState<number>("width");
            const height = useState<number>("height");
            this.width = width.value;
            this.height = height.value;

            if (this.snapshots.length > this.width * 2) {
                // clean up once ina while
                this.snapshots = this.snapshots.slice(this.snapshots.length - this.width);
            }

            if (this.snapshots.length == 0)
                return;


            // we will use the top bid of the newest snapshot as the baseline

            for (let x = 1; x < this.snapshots.length; x++) {
                if (x > this.width)
                    break;

                const s = this.snapshots[this.snapshots.length - x];
                if (x == 1) {
                    this.baseLine = s.b[0].p;
                }

                this.draw(this.width - x, s, this.baseLine);
            }

        },
        round(x: number) {
            return +(Math.round(x + "e+" + this.tickDigits) + "e-" + this.tickDigits)
        },
        draw(x: number, s: OrderBookSnapshot, baseLine: number) {
            if (this.c === undefined)
                return;

            const output = new Array<Quote>(this.height);

            let curPrice = this.round(baseLine + (this.minTick * this.height / 2));
            let minPrice = this.round(baseLine - (this.minTick * this.height / 2));

            // todo this takes around 14ms, optimize

            let all: Quote[] = new Array<number>(this.height);
            // let all: Quote[] = [...s.a, ...s.b];

            let aindex = 0;
            while (s.a[s.a.length - aindex - 1].p <= curPrice) {
                aindex++;
            }
            aindex++;

            let bindex = 0;
            while (s.b[bindex].p >= minPrice) {
                bindex++;
            }
            bindex++;

            if (aindex < 0) {
                all = s.b.slice(0, bindex);
            }
            else if (bindex < 0) {
                all = s.a.slice(0, aindex);
            }
            else {
                // could extract a milisecond maybe for the entire draw by doing this 
                // for(let i = 0; i < aindex; i++)
                // {
                // 
                // }
                all = [...s.a.slice(s.a.length - aindex), ...s.b.slice(0, bindex)];
            }

            let allIndex = 0;
            for (let i = 0; i < all.length; i++) {
                const cur = all[i];
                if (cur.p <= curPrice) {
                    allIndex = i;
                    break;
                }
            }
            let outputIndex = 0;
            while (outputIndex < output.length) {
                while (all[allIndex].p < this.round(curPrice - outputIndex * this.minTick)) {
                    output[outputIndex++] = emptyQuote(all[allIndex].p);

                    if (outputIndex == output.length)
                        break;
                }

                if (outputIndex == output.length)
                    break;

                allIndex++;


                if (allIndex <= aindex) {
                    output[outputIndex++] = { q: -all[allIndex].q, p: all[allIndex].p };
                }
                else {
                    output[outputIndex++] = { q: all[allIndex].q, p: all[allIndex].p };
                }
            }

            const maxSize = output.reduce((a, b) => Math.max(a, Math.abs(b.q)), 0);
            const imageDataArr = new Uint8ClampedArray(this.height * 4);

            const cRect = this.c.getBoundingClientRect();
            const selPos = {
                x: this.mousePos.x - cRect.left,
                y: this.mousePos.y //+ cRect.top,
            };

            // console.log(this.c.clientWidth)
            selPos.x = Math.floor(selPos.x * (this.width / this.c.clientWidth));
            selPos.y = Math.floor(selPos.y * (this.height / this.c.clientHeight));

            // console.log(selPos)

            for (let y = 0; y < output.length; y++) {
                const { q, p } = output[y];
                // let zero = 0;
                if (this.showPrice && selPos.x == x && selPos.y == y) {
                    this.selectedData = {
                        p,
                        q
                    };

                    imageDataArr[y * 4 + 0] = 255;
                    imageDataArr[y * 4 + 1] = 255;
                    imageDataArr[y * 4 + 2] = 255;
                    imageDataArr[y * 4 + 3] = 255;
                    continue;
                }
                else if (this.showPrice && q != 0 && q * q == this.selectedData.q * this.selectedData.q) {
                    imageDataArr[y * 4 + 0] = q < 0 ? 255 : 200;
                    imageDataArr[y * 4 + 1] = q > 0 ? 255 : 200;
                    imageDataArr[y * 4 + 2] = 200;
                    imageDataArr[y * 4 + 3] = 255;
                }
                else {
                    imageDataArr[y * 4 + 0] = q < 0 ? 255 : 0;
                    imageDataArr[y * 4 + 1] = q > 0 ? 255 : 0;
                    imageDataArr[y * 4 + 2] = 0;
                    imageDataArr[y * 4 + 3] = 255 * this.calculateColor(0, maxSize, Math.abs(q));
                }

                // if(this.showPrice &&)

                // data[0] = cur < 0 ? 255 : 0;
                // data[1] = cur >= 0 ? 255 : 0;
                // data[2] = 0;
                // data[3] = 255 * this.calculateColor(0, maxSize, Math.abs(cur));
                // this.ctx?.putImageData(this.imageData, x, y);
            }

            if (this.detailsElem.clientHeight + this.mousePos.y + 5 >= window.innerHeight) {
                this.detailsElem.style.top = this.mousePos.y - this.detailsElem.clientHeight - 5 + "px";
            }
            else {
                this.detailsElem.style.top = this.mousePos.y + "px";
            }

            if (this.detailsElem.clientWidth + this.mousePos.x + 5 >= window.innerWidth) {
                this.detailsElem.style.left = window.innerWidth - this.detailsElem.clientWidth - 10 + "px";
            }
            else {
                this.detailsElem.style.left = this.mousePos.x + "px";
            }

            this.ctx?.putImageData(new ImageData(imageDataArr, 1, this.height), x, 0);
        },
        calculateColor(minSize: number, maxSize: number, amount: number) {
            if (amount == 0)
                return 0;

            // return 0.5;
            // evertyhing i tried is ugly, the best idea woudl be to have 3 colors
            // one for 1/3 or less, one for 2/3 or less and one for 3/3
            const normal = (amount + (maxSize - amount) / 15) / (maxSize);

            return normal;
        },
        drawSquare() {

        },
        onDataReceived(s: OrderBookSnapshot) {
            // console.timeEnd("hehe");
            // const end = new Date();
            // const milis = (end - this.measure);
            // if(milis > 150)
            //     console.log(milis);

            if (!this.isStopped)
                this.snapshots.push(s);
            // this.measure = end;
            // console.time("hehe");
        }
    }
};

</script>

<style scoped>
canvas {
    image-rendering: pixelated;
}
</style>