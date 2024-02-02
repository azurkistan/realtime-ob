<template>
    <canvas id="c" :width="w" :height="h" class="bg-black w-full h-full">

        <div class="top-0 right-[-50px] p-4 bg-white text-white">
            This is some text that will be displayed on top of the parent div, positioned at the right edge.
        </div>
    </canvas>
</template>

<script lang="ts" setup>

const w = useState("width", () => 35);
const h = useState("height", () => 80);

</script>

<script lang="ts">
import * as signalR from "@microsoft/signalr";
import type OrderBookSnapshot from "./types/OrderBookSnapshot";
import type Quote from "./types/Quote";
import ConnectionState from "./types/ConnectionState";

function emptyQuote(n: number): Quote { return ({ p: n, q: 0 }) };
export default {
    data() {
        return {
            timer: {} as NodeJS.Timeout,
            connection: {} as signalR.HubConnection,
            imageData: {} as ImageData,
            c: {} as HTMLCanvasElement,
            ctx: undefined as (CanvasRenderingContext2D | undefined),
            isStopped: false,
            snapshots: [] as OrderBookSnapshot[],
            width: 25, // todo this should be in the state and bidirectional
            height: 50, // same as above
            minTick: 0.001, // same ^
            // pauseListener: {},
        };
    },
    mounted() {
        const c = document.getElementById("c") as HTMLCanvasElement;
        const ctx = c.getContext("2d") as CanvasRenderingContext2D;

        ctx.imageSmoothingEnabled = false;

        this.ctx = ctx;
        this.c = c;

        // we do this for drawing speed
        this.imageData = this.ctx.createImageData(1, 1);

        // todo update as width/height change
        // note in teh future, i might want to scale this to allow drawing or whatever


        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/ws")
            .withAutomaticReconnect([1000, 10000, 30000])
            .build();

        this.connection.on('upd', this.onDataReceived);
        const connState = useState<ConnectionState>("connectionState", () => ConnectionState.Disconnected);
        const symbol = useState("symbol", () => "ordiusdt");

        this.connection.onreconnecting(() => {
            connState.value = ConnectionState.Connecting;
        });
        this.connection.onclose(() => {
            connState.value = ConnectionState.Disconnected;
        });
        this.connection.onreconnected(() => {
            connState.value = ConnectionState.Connected;
            this.ctx?.clearRect(0, 0, this.c.width, this.c.height);
            this.connection.send("sub", symbol.value);
        });


        // available only in the DLC
        // document.addEventListener("keypress", (e) => {
        //     if (e.code == "Space") {
        //         this.isStopped = !this.isStopped;
        //     }
        // });

        this.timer = setInterval(() => {
            if (this.isStopped)
                return;

            console.time("draw");
            this.drawAll();
            console.timeEnd("draw")
        }, 1000 / 12);

        this.connection.start()
            .then(() => {
                let conn = useState<ConnectionState>("connectionState")
                conn.value = ConnectionState.Connected;
                this.connection.send("sub", symbol.value);
            });
    },
    beforeUnmount() {
        this.connection.stop();
        clearInterval(this.timer);
    },
    computed: {
        symbol() {
            return useState("symbol", () => "ordiusdt");
        }
    },
    watch: {
        symbol(oldSymbol, newSymbol) {
            this.ctx?.clearRect(0, 0, this.c.width, this.c.height);
            this.connection.send("sub", newSymbol.value);
        }
    },
    methods: {
        drawAll() {
            // draw the last width worth of snapshots

            const width = useState<number>("width");
            const height = useState<number>("height");
            this.width = width.value;
            this.height = height.value;

            // if (this.c.width != this.width || this.c.height != this.height) {
            //     this.c.width = this.width;
            //     this.c.height = this.height;
            // }

            if (this.snapshots.length > this.height * 2) {
                // clean up once ina while
                this.snapshots = this.snapshots.slice(this.snapshots.length - 50);
            }

            if (this.snapshots.length == 0)
                return;


            // we will use the top bid of the newest snapshot as the baseline


            let baseLine = 0;
            for (let x = 1; x < this.snapshots.length; x++) {
                if (x > this.width)
                    break;

                const s = this.snapshots[this.snapshots.length - x];
                if (x == 1) {
                    baseLine = s.b[0].p;
                    this.minTick = Math.min(Math.abs(s.b[0].p - s.b[1].p), Math.abs(s.a[s.a.length - 1].p - s.a[s.a.length - 2].p));
                    const minTick = useState("tickSize")
                    minTick.value = this.minTick;
                }

                this.draw(this.width - x, s, baseLine);
            }
        },
        draw(x: number, s: OrderBookSnapshot, baseLine: number) {
            if (this.c === undefined)
                return;
            const output = new Array<number>(this.height);


            // todo this takes around 14ms, optimize
            const all: Quote[] = [...s.a, ...s.b];


            let allIndex = 0;
            let curPrice = baseLine + (this.minTick * this.height / 2);
            for (const i in all) {
                const cur = all[i];
                if (cur.p <= curPrice) {
                    allIndex = parseInt(i);
                    break;
                }
            }

            let outputIndex = 0;
            while (outputIndex < output.length) {
                while (all[allIndex].p < curPrice - outputIndex * this.minTick) {
                    output[outputIndex++] = 0;
                }

                output[outputIndex++] = all[allIndex++].q;

                if (allIndex <= s.a.length) {
                    output[outputIndex - 1] = -output[outputIndex - 1];
                }
            }

            const maxSize = output.reduce((a, b) => Math.max(a, Math.abs(b)), 0);

            const data = this.imageData.data;
            for (let y = 0; y < output.length; y++) {
                const cur = output[y];
                data[0] = cur < 0 ? 255 : 0;
                data[1] = cur >= 0 ? 255 : 0;
                data[2] = 0;
                data[3] = 255 * this.calculateColor(0, maxSize, Math.abs(cur));
                this.ctx?.putImageData(this.imageData, x, y + 1);
            }
        },
        calculateColor(minSize: number, maxSize: number, amount: number) {
            if (amount == 0)
                return 0;

            // evertyhing i tried is ugly, the best idea woudl be to have 3 colors
            // one for 1/3 or less, one for 2/3 or less and one for 3/3
            amount = amount + 10 * (1 - (amount / maxSize))
            const normal = (amount) / (maxSize);

            return normal;
        },
        drawSquare() {

        },
        onDataReceived(s: OrderBookSnapshot) {
            this.snapshots.push(s);
        }
    }
};

</script>

<style scoped>
canvas {
    image-rendering: pixelated;
}
</style>