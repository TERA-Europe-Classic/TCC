const { Globals } = require("../global/lib/globals");

class TccStub {
    constructor(mod) {
        this.mod = mod;

        Globals.setCommand(mod.command);

        this.globalMod().setNetworkMod(this);

        this.installHooks();

        //memes
        this.mod.hook("S_SYSTEM_MESSAGE", 1, ev => {
            if (mod.game.me.inDungeon === true || mod.game.me.inBattleground === true) return;
            if (ev.message.indexOf("Foglio") === -1) return;

            const sm = mod.parseSystemMessage(ev.message);
            if (sm.id === "SMT_FRIEND_SEND_HELLO"
                || sm.id === "SMT_FRIEND_RECEIVE_HELLO") {
                this.memeA();
            }
        });

        this.mod.command.add("tcc", (cmd, a1, a2, a3, a4) => {
            switch (cmd) {
                case "debug":
                    {
                        mod.settings.debug = !mod.settings.debug;
                        mod.command.message(`<font color="#cccccc">Debug mode </font><font color="#${(mod.settings.debug ? "42F5AD" : "F05164")}">${(mod.settings.debug ? "en" : "dis")}abled</font>`);
                        break;
                    }
                case "notify":
                    {
                        this.globalMod().call("enqueueNotification",
                            {
                                'title': a1,
                                'message': a2,
                                'notificationType': a3,
                                'secDuration': a4
                            });
                        mod.command.message(`<font color="#cccccc">Sending notification: </font><font color="#42F5AD">${a1} ${a2} ${a3} ${a4}</font>`);

                        break;
                    }
                default:
                    break;
            }


        });

        this.mod.command.add(":tcc-uimode", (arg) => {
            Globals.debug("Setting UiMode to " + arg);
            this.globalMod().call("setUiMode", { 'uiMode': arg == "true" });
        });

        this.mod.command.add(":tcc-proxyOn:", (arg) => { });

    }

    globalMod() {
        return this.mod.globalMod;
    }

    installHooks() {
        // block ingame player menu
        this.mod.hook("S_ANSWER_INTERACTIVE", "raw", () => { return !Globals.EnablePlayerMenu; });
        // register private proxy channels (like /7 and /8)
        this.mod.hook("S_JOIN_PRIVATE_CHANNEL", "raw", { order: 999, filter: { fake: true } }, (code, data, fromServer) => {
            this.globalMod().call("handleRawPacket", {
                'direction': fromServer ? 2 : 1,
                'content': data.toString("hex")
            });
            return true;
        });
    }

    memeA() {
        this.mod.send("S_USER_EFFECT", 1, {
            target: this.mod.game.me.gameId,
            source: 0,
            circle: 2,
            operation: 1
        });
        this.mod.setTimeout(() => {
            this.mod.send("S_USER_EFFECT", 1, {
                target: this.mod.game.me.gameId,
                source: 0,
                circle: 2,
                operation: 2
            });
        }, 10000);
    }

    destructor() {
        //this.globalMod().stopServer();
    }
}

exports.TccStub = TccStub;
